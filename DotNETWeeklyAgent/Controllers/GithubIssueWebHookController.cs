using DotNETWeeklyAgent.Models;
using DotNETWeeklyAgent.Options;
using DotNETWeeklyAgent.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DotNETWeeklyAgent.Controllers;


[ApiController]
[Route("issue")]
public class GithubIssueWebHookController : ControllerBase
{
    private readonly IBackgroundTaskQueue<IssueMetadata> _backgroundTaskQueue;

    private readonly ISecretTokenValidator _secretTokenValidator;

    private readonly GithubOptions _githubOptions;

    public GithubIssueWebHookController(IBackgroundTaskQueue<IssueMetadata> backgroundTaskQueue, ISecretTokenValidator secretTokenValidator, IOptions<GithubOptions> githubOptionsAccessor)
    {
        _backgroundTaskQueue = backgroundTaskQueue;
        _secretTokenValidator = secretTokenValidator;
        _githubOptions = githubOptionsAccessor.Value;
    }

    [HttpPost("event")]
    public async Task<IActionResult> Post([FromBody]IssuePayload issuePayload)
    {
#if !DEBUG
        if (!(await _secretTokenValidator.Validate(HttpContext, _githubOptions.SecretToken)))
        {
            return Forbid("Invalid request.");
        }
#endif
        if (!issuePayload.Action.Equals("opened"))
        {
            return NoContent();
        }
        IssueMetadata issueMetadata = new IssueMetadata
        {
            Owner = issuePayload.Organization.Login,
            Repo = issuePayload.Repository.Name,
            IssueNumber = issuePayload.Issue.Number,
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
