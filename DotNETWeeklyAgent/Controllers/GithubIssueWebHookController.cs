using DotNETWeeklyAgent.Models;
using DotNETWeeklyAgent.Services;

using Microsoft.AspNetCore.Mvc;

namespace DotNETWeeklyAgent.Controllers;


[ApiController]
[Route("issue")]
public class GithubIssueWebHookController : ControllerBase
{
    private readonly IBackgroundTaskQueue<IssueMetadata> _backgroundTaskQueue;

    private readonly ILogger<GithubIssueWebHookController> _logger;

    public GithubIssueWebHookController(IBackgroundTaskQueue<IssueMetadata> backgroundTaskQueue, ILogger<GithubIssueWebHookController> logger)
    {
        _backgroundTaskQueue = backgroundTaskQueue;
        _logger = logger;
    }

    [HttpPost("event")]
    public async Task<IActionResult> Post()
    {
        IssuePayload? issuePayload;
        try
        {
            issuePayload = await Request.ReadFromJsonAsync<IssuePayload>();
        }
        catch (Exception)
        {
            return NoContent();
        }

        if (!CanIssueProceed(issuePayload))
        {
            return NoContent();
        }

        IssueMetadata issueMetadata = new IssueMetadata
        {
            Action = issuePayload.Action,
            Owner = issuePayload.Organization.Login,
            Repo = issuePayload.Repository.Name,
            IssueNumber = issuePayload.Issue.Number,
            Title = issuePayload.Issue.Title,
            Link = issuePayload.Issue.Body,
            Category = ConvertIssueCategory(issuePayload.Issue.Title),
        };
        await _backgroundTaskQueue.QueueAsync(issueMetadata);
        _logger.LogInformation("Received issue: {Owner}/{Repo}#{IssueNumber} - {Title}", 
            issueMetadata.Owner, issueMetadata.Repo, issueMetadata.IssueNumber, issueMetadata.Title);
        return Ok();
    }

    private static IssueCategory ConvertIssueCategory(string title)
    {
        return title switch
        {
            var t when t.Contains("开源项目") => IssueCategory.OSS,
            var t when t.Contains("文章推荐") => IssueCategory.Article,
            var t when t.Contains("视频推荐") => IssueCategory.Video,
            var t when t.Contains("行业资讯") => IssueCategory.News,
            _ => IssueCategory.None,
        };
    }

    private bool CanIssueProceed(IssuePayload? issuePayload)
    {
        return issuePayload switch
        {
            { Action: "opened" } => true,
            { Action: "labeled" } and { Label.Name : "ImageRequired" } => true,
            _ => false
        };
    }
}
