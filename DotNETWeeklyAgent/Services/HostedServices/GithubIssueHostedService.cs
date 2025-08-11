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

            var issueFetchAgent = CreateIssueFetchAgent(kernal);
            var issueSummaryAgent = CreateIssueSummaryAgent(kernal);
            var issueCommentAgent = CreateIssueCommentAgent(kernal);
            ChatHistory history = [];

#pragma warning disable SKEXP0110
            SequentialOrchestration orchestration = new(issueFetchAgent, issueSummaryAgent, issueCommentAgent)
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
            """;

            var result = await orchestration.InvokeAsync(
        $"Can you add this summary of this issue link? {input}", runtime);

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

    private ChatCompletionAgent CreateIssueFetchAgent(Kernel kernel)
    {
        var cloneKernel = kernel.Clone();
        cloneKernel.Plugins.AddFromObject(_githubAPIService);
        ChatCompletionAgent issueFetchAgent = new ChatCompletionAgent()
        {
            Name = "IssueFetch",
            Instructions = """
            你是一个 GitHub 分析师。给定一个 GitHub 仓库的 issue，包括 owner、repo 、issue_number 和 category。你的任务是获取 issue 的属性：
            - owner
            - repo
            - issue_number
            - cateogry
            - link
            """,
            Description = "An agent to get an issue from body by owner, repo and issue_number.",
            Kernel = cloneKernel,
            Arguments = new KernelArguments(OpenAIPromptExecutionSettings),
        };

        return issueFetchAgent;
    }

    private ChatCompletionAgent CreateIssueSummaryAgent(Kernel kernel)
    {
        var cloneKernel = kernel.Clone();
        cloneKernel.Plugins.AddFromObject(_webContentService);
        cloneKernel.Plugins.AddFromObject(_youtubeTranscriptService);
        ChatCompletionAgent issueSummaryAgent = new ChatCompletionAgent()
        {
            Name = "IssueSummary",
            Instructions = """
            你是一个技术写作专家，给定一个 GitHub 仓库的 issue，包括 owner、repo 、issue_number、category 和 link。你的任务是获取 link 的内容，并将其总结为一段总结，请注意以下几点：
            1. 如果 issue 的 category 是 `article`、`OSS` 和 `News` ，请获取链接文章内容并进行总结。
            2. 如果 issue 的 category 是 `video`，请获取 YouTube 的 transcript，然后进行总结。
            3. 总结内容应使用中文，格式为 Markdown，但不要使用标题样式（如 h1、h2 等等）。
            """,
            Description = "An agent to create a summary based on link.",
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
            Instructions = """
            你是一个 github 专家，给定一个 github 仓库的 issue，包含 owner, repo, issue_number 和 body, 你的任务是将这个 body 作为 github 的 comment。注意，这个 body 是这个 issue 内容的总结
            """,
            Description = "An an agent to add an comment to github repo issue.",
            Kernel = cloneKernel,
            Arguments = new KernelArguments(OpenAIPromptExecutionSettings),
        };

        return issueCommentAgent;
    }
}
