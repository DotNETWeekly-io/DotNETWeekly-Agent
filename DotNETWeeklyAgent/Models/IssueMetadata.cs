using System.Text.Json.Serialization;

namespace DotNETWeeklyAgent.Models;

public class IssueMetadata
{
    [JsonPropertyName("organization")]
    public required string Organization { get; set; }

    [JsonPropertyName("repository")]
    public required string Repository { get; set; }

    [JsonPropertyName("id")]
    public required int Id { get; set; }

    [JsonPropertyName("category")]
    public required IssueCategory Category { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("link")]
    public required string Link { get; set; }
}
