using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using static System.Net.Mime.MediaTypeNames;

namespace SemanticKernalPlayground;

public class IssueMultiAgents
{
    private readonly AzureOpenAIOptions _azureOpenAIOptions;

    private readonly GithubOptions _githubOptions;

    private readonly ILoggerFactory _loggerFactory;

    private readonly WebContentService _webContentService;


    public IssueMultiAgents(IOptions<AzureOpenAIOptions> azureOpenAIOptionsAccessor, IOptions<GithubOptions> githubOptionsAccessor, ILoggerFactory loggerFactory, WebContentService webContentService)
    {
        _azureOpenAIOptions = azureOpenAIOptionsAccessor.Value;
        _githubOptions = githubOptionsAccessor.Value;
        _loggerFactory = loggerFactory;
        _webContentService = webContentService;
    }


    public async Task RunAsync()
    {
        // Initialize the kernel and add the necessary services and tools
        IKernelBuilder builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<ILoggerFactory>(_loggerFactory);
        builder.AddAzureOpenAIChatCompletion(_azureOpenAIOptions.DeploymentName, _azureOpenAIOptions.Endpoint, _azureOpenAIOptions.APIKey, modelId: _azureOpenAIOptions.ModelId);
        var kernel = builder.Build();

        ChatCompletionAgent issueFetchAgent = CreateIssueFetchAgent(kernel);

        ChatCompletionAgent issueCommentAgent = CreateIssueCommentAgent(kernel);

        ChatCompletionAgent issueUpdateAgent = CreateIssueUpdateAgent(kernel);

        ChatHistory history = [];

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        SequentialOrchestration orchestration = new(issueFetchAgent, issueCommentAgent, issueUpdateAgent)
        {
            ResponseCallback = responseCallback,
        };

        InProcessRuntime runtime = new InProcessRuntime();
        await runtime.StartAsync();

        var input = """
            owner: DotNETWeekly-io
            repo: DotNETWeekly
            issue_number: 984
            """;

        var result = await orchestration.InvokeAsync(
    $"Can you add this summary of this issue link? {input}", runtime);

        string output = await result.GetValueAsync(TimeSpan.FromMinutes(20));
        Console.WriteLine($"\n# RESULT: {output}");
        Console.WriteLine("\n\nORCHESTRATION HISTORY");
        foreach (ChatMessageContent message in history)
        {
            Console.WriteLine(message.Content);
        }

#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        ValueTask responseCallback(ChatMessageContent response)
        {
            history.Add(response);
            return ValueTask.CompletedTask;
        }
    }

    private ChatCompletionAgent CreateIssueFetchAgent(Kernel kernel)
    {
        var clonekernel = kernel.Clone();
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
        var filteredGithubTools = githubTools.ToList();
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        clonekernel.Plugins.AddFromFunctions("githubTools", filteredGithubTools.Select(tool => tool.AsKernelFunction()));
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        };
        ChatCompletionAgent issueFetchAgent = new ChatCompletionAgent()
        {
            Name = "IssueFetch",
            Instructions = "You're a github analyst. Given you a github repo issue, including owner, repo and issue_number. Please get the issue property: \n- owner\n- repo\n- issue_number\n- link.",
            Description = "An agent to get the an issue link from body.",
            Kernel = clonekernel,
            Arguments = new KernelArguments(openAIPromptExecutionSettings),
        };

        return issueFetchAgent;
    }

    private ChatCompletionAgent CreateIssueCommentAgent(Kernel kernel)
    {
        var cloneKernel = kernel.Clone();
        cloneKernel.Plugins.AddFromObject(_webContentService);
        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        };
        ChatCompletionAgent issueCommentAgent = new ChatCompletionAgent()
        {
            Name = "WebContent",
            Instructions = "You are a web content writer. Given you a github repo issue link, please get the summary of of web link content with markdown format. Please identify the issue property: \n- owner\n- repo\n- issue_number\n- summary.",
            Description = "An agent to get the summary of web content.",
            Kernel = cloneKernel,
            Arguments = new KernelArguments(openAIPromptExecutionSettings),
        };
        return issueCommentAgent;
    }

    private ChatCompletionAgent CreateIssueUpdateAgent(Kernel kernel)
    {
        var cloneKernel = kernel.Clone();
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
        var filteredGithubTools = githubTools.ToList();
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        cloneKernel.Plugins.AddFromFunctions("githubTools", filteredGithubTools.Select(tool => tool.AsKernelFunction()));
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        };
        ChatCompletionAgent issueUpdateAgent = new ChatCompletionAgent()
        {
            Name = "IssueUpdate",
            Instructions = "You're a github analyst. Given you a github repo issue and the summary of the web link content. Can you add it as github repo issue's comment?",
            Description = "An agent to update the issue with the summary of web content.",
            Kernel = cloneKernel,
            Arguments = new KernelArguments(openAIPromptExecutionSettings),
        };
        return issueUpdateAgent;
    }
}
