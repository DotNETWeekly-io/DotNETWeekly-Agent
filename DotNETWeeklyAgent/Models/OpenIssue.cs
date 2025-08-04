using System.Text.Json.Serialization;

namespace DotNETWeeklyAgent.Models;

public class OpenIssue
{
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("number")]
    public required int Number { get; set; }

    [JsonPropertyName("body")]
    public required string Link { get; set; }

    public string Content { get; set; } = string.Empty;

    public IssueCategory IssueCategory => ConvertIssueCategory(Title);

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

public class IssueComment
{
    [JsonPropertyName("body")]
    public required string Comment { get; set; }
}
