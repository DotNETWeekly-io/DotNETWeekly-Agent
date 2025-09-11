namespace DotNETWeeklyAgent.Options;

public sealed class GithubOptions
{
    public required string APIUrl { get; set; }

    public required string MCPUrl { get; set; }

    public required string PAT { get; set; }

    public required string SecretToken { get; set; }
}
