
using DotNETWeeklyAgent.Models;

using System.Runtime.CompilerServices;

namespace DotNETWeeklyAgent.Services;

public class GithubIssueOpenHostedService : BackgroundService
{
    private readonly ILogger<GithubIssueOpenHostedService> _logger;

    private readonly IBackgroundTaskQueue<IssueMetadata> _taskQueue;

    public GithubIssueOpenHostedService(IBackgroundTaskQueue<IssueMetadata> taskQueue, ILogger<GithubIssueOpenHostedService> logger)
    {
        _taskQueue = taskQueue;
        _logger = logger;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var task in GetIssueMetaDataAsync(stoppingToken))
        {
            await Task.Delay(100, stoppingToken);
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
