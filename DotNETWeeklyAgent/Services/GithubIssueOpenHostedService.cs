
using DotNETWeeklyAgent.Models;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DotNETWeeklyAgent.Services;

public class GithubIssueOpenHostedService : BackgroundService
{
    private readonly ILogger<GithubIssueOpenHostedService> _logger;

    private readonly IBackgroundTaskQueue<IssueMetadata> _taskQueue;

    private readonly IServiceScopeFactory _serviceScopeFactory;

    public GithubIssueOpenHostedService(IBackgroundTaskQueue<IssueMetadata> taskQueue, IServiceScopeFactory serviceScopeFactory, ILogger<GithubIssueOpenHostedService> logger)
    {
        _taskQueue = taskQueue;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var issue in GetIssueMetaDataAsync(stoppingToken))
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            var kernal = sp.GetRequiredService<Kernel>();
            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };
            var chatCompletionService = kernal.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory(Prompts.IssuePersonaChinese);
            var input = $"你能获取这个 github issue 中链接的内容，并且将它添加到 github issue 的 comment 中吗? 注意请使用中文作为总结内容 {JsonSerializer.Serialize(issue)}";
            history.AddUserMessage(input);
            var result = await chatCompletionService.GetChatMessageContentAsync(history, executionSettings: openAIPromptExecutionSettings, kernel: kernal);
            _logger.LogInformation(result.Content);
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
}
