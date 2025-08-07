using DotNETWeeklyAgent.Models;
using DotNETWeeklyAgent.Services;

using Microsoft.AspNetCore.Mvc;

using System.Text.RegularExpressions;

namespace DotNETWeeklyAgent.Controllers;

[ApiController]
[Route("milestone")]
public class GithubMilestoneWebHookController : ControllerBase
{
    private readonly IBackgroundTaskQueue<MilestoneMetadata> _backgroundTaskQueue;

    private readonly ILogger<GithubMilestoneWebHookController> _logger;

    private static string[] chineseMonths = {
        "一月份", "二月份", "三月份", "四月份", "五月份", "六月份",
        "七月份", "八月份", "九月份", "十月份", "十一月份", "十二月份"
    };


    public GithubMilestoneWebHookController(IBackgroundTaskQueue<MilestoneMetadata> backgroundTaskQueue, ILogger<GithubMilestoneWebHookController> logger)
    {
        _backgroundTaskQueue = backgroundTaskQueue;
        _logger = logger;
    }

    [HttpPost("event")]
    public async Task<IActionResult> Post()
    {
        MilestonePayload? milestonePayload;
        try
        {
            milestonePayload = await Request.ReadFromJsonAsync<MilestonePayload>();
        }
        catch (Exception)
        {
            return NoContent();
        }
       
        if (milestonePayload == null || milestonePayload.Action.Equals("created"))
        {
            return NoContent();
        }

        int? number = ConvertMilestoneToNumber(milestonePayload.Milestone.Title);
        if (!number.HasValue)
        {
            _logger.LogWarning("Invalid milestone title format: {Title}. Expected format: 'episode-XXX'", milestonePayload.Milestone.Title);
            return BadRequest("Invalid milestone title format. Expected format: 'episode-XXX'.");
        }
        MilestoneMetadata milestoneMetadata = new MilestoneMetadata
        {
            Owner = milestonePayload.Organization.Login,
            Repo = milestonePayload.Repository.Name,
            Title = milestonePayload.Milestone.Title,
            Number = number.Value,
            Year = DateTime.UtcNow.Year,
            Month = chineseMonths[DateTime.UtcNow.Month - 1],
        };
        await _backgroundTaskQueue.QueueAsync(milestoneMetadata);
        _logger.LogInformation("Received milestone: {Owner}/{Repo} - {Title} (#{Number})", 
            milestoneMetadata.Owner, milestoneMetadata.Repo, milestoneMetadata.Title, milestoneMetadata.Number);
        return Ok();
    }

    private static int? ConvertMilestoneToNumber(string title)
    {
        string pattern = @"^episode-(\d{3})$";
        Match match = Regex.Match(title, pattern);
        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value);
        }
        return null;
    }
}
