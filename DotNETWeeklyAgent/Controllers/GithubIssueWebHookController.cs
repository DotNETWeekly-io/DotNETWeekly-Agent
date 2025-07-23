using DotNETWeeklyAgent.Models;
using DotNETWeeklyAgent.Services;

using Microsoft.AspNetCore.Mvc;

namespace DotNETWeeklyAgent.Controllers;


[ApiController]
[Route("/issue")]
public class GithubIssueWebHookController : ControllerBase
{
    private readonly IBackgroundTaskQueue<IssueMetadata> _backgroundTaskQueue;

    public GithubIssueWebHookController(IBackgroundTaskQueue<IssueMetadata> backgroundTaskQueue)
    {
        _backgroundTaskQueue = backgroundTaskQueue;
    }

    [HttpPost("/event")]
    public async Task<IActionResult> Post([FromBody]IssuePayload issuePayload)
    {
        if (!issuePayload.Action.Equals("opened"))
        {
            return NoContent();
        }
        IssueMetadata issueMetadata = new IssueMetadata
        {
            Organization = issuePayload.Organization.Login,
            Repository = issuePayload.Repository.Name,
            Id = issuePayload.Issue.Number,
            Title = issuePayload.Issue.Title,
            Link = issuePayload.Issue.Body,
            Category = ConvertIssueCategory(issuePayload.Issue.Title),
        };
        await _backgroundTaskQueue.QueueAsync(issueMetadata);
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
}
