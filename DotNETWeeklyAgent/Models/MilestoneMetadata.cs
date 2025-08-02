using System.Text.Json.Serialization;

namespace DotNETWeeklyAgent.Models;

public class MilestoneMetadata
{
    [JsonPropertyName("owner")]
    public required string Owner { get; set; }
    
    [JsonPropertyName("repo")]
    public required string Repo { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("number")]
    public required int Number { get; set; }

    [JsonPropertyName("year")]
    public required int Year { get; set; }

    [JsonPropertyName("month")]
    public required string Month { get; set; }
}
