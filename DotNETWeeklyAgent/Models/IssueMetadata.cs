using System.Text.Json.Serialization;

namespace DotNETWeeklyAgent.Models;

public class IssueMetadata
{
    [JsonPropertyName("owner")]
    public required string Organization { get; set; }

    [JsonPropertyName("repo")]
    public required string Repository { get; set; }

    [JsonPropertyName("issue_number")]
    public required int Id { get; set; }

    [JsonPropertyName("category")]
    public required IssueCategory Category { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("link")]
    public required string Link { get; set; }
}
