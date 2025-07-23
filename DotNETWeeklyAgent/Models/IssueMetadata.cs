namespace DotNETWeeklyAgent.Models;

public class IssueMetadata
{
    public required string Organization { get; set; }

    public required string Repository { get; set; }

    public required int Id { get; set; }

    public required IssueCategory Category { get; set; }

    public required string Title { get; set; }

    public required string Link { get; set; }
}
