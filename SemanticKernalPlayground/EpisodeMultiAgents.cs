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

namespace SemanticKernalPlayground;

public class EpisodeMultiAgents
{
    private readonly AzureOpenAIOptions _azureOpenAIOptions;

    private readonly GithubOptions _githubOptions;

    private readonly ILoggerFactory _loggerFactory;

    private readonly WebContentService _webContentService;

    private readonly GithubAPIService _githubAPIService;

    public EpisodeMultiAgents(
        IOptions<AzureOpenAIOptions> azureOpenAIOptionsAccessor,
        IOptions<GithubOptions> githubOptionsAccessor,
        ILoggerFactory loggerFactory,
        WebContentService webContentService,
        GithubAPIService githubAPIService
        )
    {
        _azureOpenAIOptions = azureOpenAIOptionsAccessor.Value;
        _githubOptions = githubOptionsAccessor.Value;
        _loggerFactory = loggerFactory;
        _webContentService = webContentService;
        _githubAPIService = githubAPIService;
    }

    public async Task RunAsync()
    {
        IKernelBuilder builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<ILoggerFactory>(_loggerFactory);
        builder.AddAzureOpenAIChatCompletion(_azureOpenAIOptions.DeploymentName, _azureOpenAIOptions.Endpoint, _azureOpenAIOptions.APIKey, modelId: _azureOpenAIOptions.ModelId);
        var kernel = builder.Build();

        var issueCollectorAgent = CreateIssueCommentsCollection(kernel);
        var episodeAgent = CreateEpisodeContentAgent(kernel);
        var pullRequestAgent = CreatePullRequestAgent(kernel);
        ChatHistory history = [];

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        SequentialOrchestration orchestration = new(issueCollectorAgent, episodeAgent, pullRequestAgent)
        {
            ResponseCallback = responseCallback,
        };

        InProcessRuntime runtime = new InProcessRuntime();
        await runtime.StartAsync();

        var input = """
            owner: DotNETWeekly-io
            repo: DotNETWeekly
            episode_number: 72
            """;

        var result = await orchestration.InvokeAsync(
    $"Can you create a Pull Request?  {input}", runtime);

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


    private ChatCompletionAgent CreateIssueCommentsCollection(Kernel kernel)
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
        var filteredGithubTools = githubTools.Where(p => p.Name.Contains("list_issues ", StringComparison.OrdinalIgnoreCase) ||
        p.Name.Contains("get_issue_comments", StringComparison.OrdinalIgnoreCase)).ToList();

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        cloneKernel.Plugins.AddFromFunctions("githubTools", filteredGithubTools.Select(tool => tool.AsKernelFunction()));
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        };

        ChatCompletionAgent issueCommentCollectionAgent = new ChatCompletionAgent()
        {
            Name = "IssueCollector",
            Instructions = "You're a github anlayst. Give you a github owner/repo and episode_number. Could collect the all open issue comments in that repo. The output should be the array. Each elements includes following properities: `Title`, `Link` and `comment`",
            Description = "An agent to collect all open issue comments",
            Kernel = cloneKernel,
            Arguments = new KernelArguments(openAIPromptExecutionSettings),
        };

        return issueCommentCollectionAgent;
    }

    private ChatCompletionAgent CreateEpisodeContentAgent(Kernel kernel)
    {
        var cloneKernel = kernel.Clone();

        ChatCompletionAgent episodeContentAgent = new ChatCompletionAgent()
        {
            Name = "ContentWriter",
            Instructions = "You're writer. ",
            Description = """
            You're an technical writer. Given you an episode_number and an array of elements, and each of them includes `Title`, `Link` and `Comment` properties.
            The `Title` includes the type of this element. There are fours types of them: `行业资讯`, `文章推荐`, `视频推荐` and `开源项目`. You job is to combine them together as output with markdown format. The requirements are
            1. Start with a heading: `# .NET 每周分享 {episode_number} 期`
            2. Create section header with above type categoryies, like `## 行业资讯`. In each section, add the corresponding elements with following format.
                 - `{index}、 [title](link)`: where index is the sequence number within that category (starting from 1). `Title` and `link` comes from the element property. 
                 - In the next line, add the comment
            """,
            Kernel = cloneKernel,
            Arguments = new KernelArguments(),
        };

        return episodeContentAgent;
    }

    private ChatCompletionAgent CreatePullRequestAgent(Kernel kernel)
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
        var filteredGithubTools = FilterMilestoneTools(githubTools);

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        cloneKernel.Plugins.AddFromFunctions("githubTools", filteredGithubTools.Select(tool => tool.AsKernelFunction()));
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        };



        ChatCompletionAgent issueCommentCollectionAgent = new ChatCompletionAgent()
        {
            Name = "PullRequestCreator",
            Instructions = """
            You're a github anlayst. Give you a github owner/repo,  epispode_number and content. Could you create complete this task?
            1. Create a new branch based on the master branch. The branch name should be geneated radonly with GUID to avoid conflicts.
            2. Then create and modify the following to file.
                - In the `/doc` directory, create a markdown file named episode-{episode_number}.md. The {episode_number} should be formatted as a three-digit number, e.g., 10 → 010, 73 → 073.
                - Update the `README.md` file by adding or modifying one line. Locate the corresponding year and month, then under the list for that month, add a new entry in the following format:
                    - [第 {episode_number} 期](./doc/episode-{episode_number}.md) — this entry links to the markdown file for the episode.
            """,
            Description = "An agent to create a pull request.",
            Kernel = cloneKernel,
            Arguments = new KernelArguments(openAIPromptExecutionSettings),
        };

        return issueCommentCollectionAgent;

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
