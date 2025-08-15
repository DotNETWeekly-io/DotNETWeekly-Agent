using DotNETWeeklyAgent.Models;
using DotNETWeeklyAgent.Options;

using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DotNETWeeklyAgent.Services.HostedServices;

public class GithubMilestoneHostedService : BackgroundService
{
    private readonly ILogger<GithubMilestoneHostedService> _logger;
    private readonly IBackgroundTaskQueue<MilestoneMetadata> _taskQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private readonly GithubAPIService _githubAPIService;

    private readonly GithubOptions _githubOptions;

    private static OpenAIPromptExecutionSettings OpenAIPromptExecutionSettings = new()
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
    };

    public GithubMilestoneHostedService(
        IBackgroundTaskQueue<MilestoneMetadata> taskQueue,
        IServiceScopeFactory serviceScopeFactory,
        GithubAPIService githubAPIService,
        IOptions<GithubOptions> githubOptionsAccessor,
        ILogger<GithubMilestoneHostedService> logger)
    {
        _taskQueue = taskQueue;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _githubAPIService = githubAPIService;
        _githubOptions = githubOptionsAccessor.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var milestone in GetMilestoneMetadataAsync(stoppingToken))
        {
            _logger.LogInformation("Processing milestone: {Milestone}", JsonSerializer.Serialize(milestone));
            using var scope = _serviceScopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            var kernel = sp.GetRequiredService<Kernel>();

            var episodeContentAgent = CreateEpisodeContentAgent(kernel);
            var episodePublishAgent = CreateEpisodePublishAgent(kernel);

            ChatHistory history = [];

#pragma warning disable SKEXP0110
            SequentialOrchestration orchestration = new(episodeContentAgent, episodePublishAgent)
            {
                ResponseCallback = responseCallback,
            };
            InProcessRuntime runtime = new InProcessRuntime();
            await runtime.StartAsync();

            var input = $"""
            owner: {milestone.Owner} 
            repo: {milestone.Repo}
            number: {milestone.Number}
            """;
            var result = await orchestration.InvokeAsync(
        $"Can you publish a github pull request as episode? {input}", runtime);

            string output = await result.GetValueAsync(TimeSpan.FromMinutes(20));
            _logger.LogInformation($"# RESULT: {output}");
            _logger.LogInformation("ORCHESTRATION HISTORY");
            foreach (ChatMessageContent message in history)
            {
                _logger.LogInformation(message.Content);
            }
#pragma warning restore SKEXP0110

            ValueTask responseCallback(ChatMessageContent response)
            {
                history.Add(response);
                return ValueTask.CompletedTask;
            }
        }
    }

    private async IAsyncEnumerable<MilestoneMetadata> GetMilestoneMetadataAsync([EnumeratorCancellation] CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            MilestoneMetadata milestoneMetadata = await _taskQueue.DequeueAsync(token);
            yield return milestoneMetadata;
        }
    }

    private ChatCompletionAgent CreateEpisodeContentAgent(Kernel kernel)
    {
        var cloneKernel = kernel.Clone();
        cloneKernel.Plugins.AddFromObject(_githubAPIService);
        ChatCompletionAgent episodeContentAgent = new ChatCompletionAgent()
        {
            Name = "EpisodeContent",
            Instructions = Prompts.EpisodeContentInstrution,
            Description = "An agent to create the episode content",
            Kernel = cloneKernel,
            Arguments = new KernelArguments(OpenAIPromptExecutionSettings),
        };

        return episodeContentAgent;
    }

    private ChatCompletionAgent CreateEpisodePublishAgent(Kernel kernel)
    {
        var cloneKernel = kernel.Clone();
        // Add github mcp server
        var sseClientTransportOptions = new SseClientTransportOptions
        {
            Name = "GitHub",
            Endpoint = new Uri(_githubOptions.MCPUrl),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string>()
                {
                    { "Authorization", $"Bearer {_githubOptions.PAT}" }
                }
        };
        McpClientOptions githubClientOptions = new McpClientOptions
        {
            ClientInfo = new Implementation { Name = "Github Client", Version = "1.0.0" }
        };
        IClientTransport clientTransport = new SseClientTransport(sseClientTransportOptions);
        IMcpClient githubMcpClient = McpClientFactory.CreateAsync(clientTransport, githubClientOptions).ConfigureAwait(false).GetAwaiter().GetResult();
        var githubTools = githubMcpClient.ListToolsAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        var filteredGithubTools = FilterMilestoneTools(githubTools).ToList();
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        cloneKernel.Plugins.AddFromFunctions("githubTools", filteredGithubTools.Select(tool => tool.AsKernelFunction()));
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        cloneKernel.Plugins.AddFromObject(_githubAPIService);

        ChatCompletionAgent episodePublishAgent = new ChatCompletionAgent
        {
            Name = "EpisodeContent",
            Instructions = Prompts.EpisodePublishInstruction,
            Description = "An agent to publish the episode",
            Kernel = cloneKernel,
            Arguments = new KernelArguments(OpenAIPromptExecutionSettings)
        };

        return episodePublishAgent;

    }

    private static List<McpClientTool> FilterMilestoneTools(IList<McpClientTool> tools)
    {
        return tools.Where(tool => tool.Name.Contains("create_branch", StringComparison.OrdinalIgnoreCase) ||
                                   tool.Name.Contains("push_files", StringComparison.OrdinalIgnoreCase) ||
                                   tool.Name.Contains("create_or_update_file", StringComparison.OrdinalIgnoreCase) ||
                                   tool.Name.Contains("create_pull_request", StringComparison.OrdinalIgnoreCase) ||
                                   tool.Name.Contains("create_pull_request_with_copilot ", StringComparison.OrdinalIgnoreCase) ||
                                   tool.Name.Contains("get_file_contents", StringComparison.OrdinalIgnoreCase)
                          ).ToList();
    }
}
