namespace DotNETWeeklyAgent.Options;

public class AzureOpenAIOptions
{
    public  required string DeploymentName { get; set; }

    public required string ImageDeploymentName { get; set; }

    public required string Endpoint { get; set; }

    public required string ModelId { get; set; }

    public required string APIKey { get; set; }
}
