using DotNETWeeklyAgent.Models;
using DotNETWeeklyAgent.SK;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DotNETWeeklyAgent.Services.HostedServices;

public class GithubIssueHostedService : BackgroundService
{
    private readonly ILogger<GithubIssueHostedService> _logger;

    private readonly IBackgroundTaskQueue<IssueMetadata> _taskQueue;

    private readonly IServiceScopeFactory _serviceScopeFactory;

    private readonly GithubAPIService _githubAPIService;

    private readonly WebContentService _webContentService;

    private readonly YoutubeTranscriptService _youtubeTranscriptService;

    private static OpenAIPromptExecutionSettings OpenAIPromptExecutionSettings = new()
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
    };

    public GithubIssueHostedService(
        IBackgroundTaskQueue<IssueMetadata> taskQueue, 
        IServiceScopeFactory serviceScopeFactory,
        GithubAPIService githubAPIService,
        WebContentService webContentService,
        YoutubeTranscriptService youtubeTranscriptService,
        ILogger<GithubIssueHostedService> logger)
    {
        _taskQueue = taskQueue;
        _logger = logger;
        _githubAPIService = githubAPIService;
        _webContentService = webContentService;
        _youtubeTranscriptService = youtubeTranscriptService;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var issue in GetIssueMetaDataAsync(stoppingToken))
        {
            _logger.LogInformation("Processing issue: {Issue}", JsonSerializer.Serialize(issue));
            using var scope = _serviceScopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            var kernal = sp.GetRequiredKeyedService<Kernel>(nameof(KernalType.Issue));

            var issueSummaryAgent = CreateIssueSummaryAgent(kernal);
            var issueCommentAgent = CreateIssueCommentAgent(kernal);
            ChatHistory history = [];

#pragma warning disable SKEXP0110
            SequentialOrchestration orchestration = new(issueSummaryAgent, issueCommentAgent)
            {
                ResponseCallback = responseCallback,
            };
            InProcessRuntime runtime = new InProcessRuntime();
            await runtime.StartAsync();

            var input = $"""
            owner: {issue.Owner} 
            repo: {issue.Repo}
            issue_number: {issue.IssueNumber}
            category: {issue.Category.ToString()}
            link: {issue.Link}
            """;

            var result = await orchestration.InvokeAsync(
        $"Can you add this summary of this github issue link? {input}", runtime);

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

    private async IAsyncEnumerable<IssueMetadata> GetIssueMetaDataAsync([EnumeratorCancellation] CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            IssueMetadata issueMetadata = await _taskQueue.DequeueAsync(token);
            yield return issueMetadata;
        }
    }

    private ChatCompletionAgent CreateIssueSummaryAgent(Kernel kernel)
    {
        var cloneKernel = kernel.Clone();
        cloneKernel.Plugins.AddFromObject(_webContentService);
        cloneKernel.Plugins.AddFromObject(_youtubeTranscriptService);
        ChatCompletionAgent issueSummaryAgent = new ChatCompletionAgent()
        {
            Name = "IssueSummary",
            Instructions = Prompts.IssueSummaryInstrution,
            Description = "An agent to create a summary based on github issue's link.",
            Kernel = cloneKernel,
            Arguments = new KernelArguments(OpenAIPromptExecutionSettings),
        };
        return issueSummaryAgent;
    }

    private ChatCompletionAgent CreateIssueCommentAgent(Kernel kernel)
    {
        var cloneKernel = kernel.Clone();
        cloneKernel.Plugins.AddFromObject(_githubAPIService);
        ChatCompletionAgent issueCommentAgent = new ChatCompletionAgent()
        {
            Name = "IssueComment",
            Instructions =Prompts.IssueCommentInstruction,
            Description = "An an agent to add an comment to github repo issue.",
            Kernel = cloneKernel,
            Arguments = new KernelArguments(OpenAIPromptExecutionSettings),
        };

        return issueCommentAgent;
    }
}
