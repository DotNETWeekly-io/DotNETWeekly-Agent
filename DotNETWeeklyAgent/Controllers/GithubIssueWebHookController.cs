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
            Category = "文章推荐"
        };
        await _backgroundTaskQueue.QueueAsync(issueMetadata);
        return Ok();
    }
}
