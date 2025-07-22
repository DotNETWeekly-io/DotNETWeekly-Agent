namespace DotNETWeeklyAgent.Models;

public class IssuePayload
{
    public required string Action { get; set; }

    public required Issue Issue { get; set; }

    public required Repository Repository { get; set; }

    public required Organization Organization { get; set; }

}

public class Issue
{
    public required string Title { get; set; }

    public required int Number { get; set; }

    public required string Body { get; set; }
}

public class Repository
{
    public required string Name { get; set; }
}

public class Organization
{
    public required string Login { get; set; }
}
