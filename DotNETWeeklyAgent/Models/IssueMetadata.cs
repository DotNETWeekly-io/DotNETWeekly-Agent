using System.Text.Json.Serialization;

namespace DotNETWeeklyAgent.Models;

public class IssueMetadata
{
    [JsonPropertyName("owner")]
    public required string Owner { get; set; }

    [JsonPropertyName("repo")]
    public required string Repo { get; set; }

    [JsonPropertyName("issue_number")]
    public required int IssueNumber { get; set; }

    [JsonPropertyName("category")]
    public required IssueCategory Category { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("link")]
    public required string Link { get; set; }
}
