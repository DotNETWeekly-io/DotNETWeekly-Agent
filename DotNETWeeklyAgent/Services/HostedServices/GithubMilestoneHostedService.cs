using DotNETWeeklyAgent.Models;
using DotNETWeeklyAgent.SK;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DotNETWeeklyAgent.Services.HostedServices;

public class GithubMilestoneHostedService : BackgroundService
{
    private readonly ILogger<GithubMilestoneHostedService> _logger;
    private readonly IBackgroundTaskQueue<MilestoneMetadata> _taskQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    public GithubMilestoneHostedService(IBackgroundTaskQueue<MilestoneMetadata> taskQueue, IServiceScopeFactory serviceScopeFactory, ILogger<GithubMilestoneHostedService> logger)
    {
        _taskQueue = taskQueue;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var milestone in GetMilestoneMetadataAsync(stoppingToken))
        {
            _logger.LogInformation("Processing milestone: {Milestone}", JsonSerializer.Serialize(milestone));
            using var scope = _serviceScopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            var kernel = sp.GetRequiredKeyedService<Kernel>(nameof(KernalType.Milestone));
            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            };

            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory(Prompts.MilestonePersonaChinese);
            var input = $"你能帮我根据这个 github repo 创建一个 PR 吗? \n {JsonSerializer.Serialize(milestone)}";
            history.AddUserMessage(input);
            await chatCompletionService.GetChatMessageContentAsync(history, executionSettings: openAIPromptExecutionSettings, kernel: kernel);
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
}
