using System.Text.Json.Serialization;

namespace DotNETWeeklyAgent.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IssueCategory
{
    None,
    Article,
    OSS,
    News,
    Video,
}
