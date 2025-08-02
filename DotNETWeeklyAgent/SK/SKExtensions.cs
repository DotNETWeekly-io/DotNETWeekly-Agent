using Azure.Identity;

using DotNETWeeklyAgent.MCPs;
using DotNETWeeklyAgent.Options;
using DotNETWeeklyAgent.Services;

using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

using ModelContextProtocol.Client;

namespace DotNETWeeklyAgent.SK;

public static class SKExtensions
{
    public static IServiceCollection AddIssueSemanticKernal(this IServiceCollection services)
    {
        services.AddKeyedSingleton(nameof(KernalType.Issue), (sp, _) =>
        {
            var azureOpenAIOptions = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;
            var kernalBuilder = Kernel.CreateBuilder()
#if DEBUG
                .AddAzureOpenAIChatCompletion(azureOpenAIOptions.DeploymentName, azureOpenAIOptions.Endpoint, azureOpenAIOptions.APIKey, modelId:azureOpenAIOptions.ModelId);
#else
                .AddAzureOpenAIChatCompletion(azureOpenAIOptions.DeploymentName, azureOpenAIOptions.Endpoint, new DefaultAzureCredential());   
#endif
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            kernalBuilder.Services.AddSingleton(loggerFactory);
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var githubAPI = new GithubAPIService(httpClientFactory, sp.GetRequiredService<ILogger<GithubAPIService>>());
            var webContent = new WebContentService(httpClientFactory, sp.GetRequiredService<ILogger<WebContentService>>());
            var youtubescript = new YoutubeTranscriptService(sp.GetRequiredService<ILogger<YoutubeTranscriptService>>());
            var kernal = kernalBuilder.Build();
            kernal.Plugins.AddFromObject(webContent);
            kernal.Plugins.AddFromObject(githubAPI);
            kernal.Plugins.AddFromObject(youtubescript);
            return kernal;
        });
        return services;
    }

    public static IServiceCollection AddMilestoneSemanticKernal(this IServiceCollection services)
    {
        services.AddKeyedSingleton(nameof(KernalType.Milestone), (sp, _) =>
        {
            var azureOpenAIOptions = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;
            var kernalBuilder = Kernel.CreateBuilder()
#if DEBUG
                .AddAzureOpenAIChatCompletion(azureOpenAIOptions.DeploymentName, azureOpenAIOptions.Endpoint, azureOpenAIOptions.APIKey, modelId:azureOpenAIOptions.ModelId);
#else
                .AddAzureOpenAIChatCompletion(azureOpenAIOptions.DeploymentName, azureOpenAIOptions.Endpoint, new DefaultAzureCredential());
#endif
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            kernalBuilder.Services.AddSingleton(loggerFactory);
            var kernal = kernalBuilder.Build();
            var githubMcpClient = GithubMCP.Create(
                    sp.GetRequiredService<IOptions<GithubOptions>>().Value,
                loggerFactory).GetAwaiter().GetResult();
            var tools = githubMcpClient.ListToolsAsync().GetAwaiter().GetResult();
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            kernal.Plugins.AddFromFunctions("githubTools", tools.Select(tool => tool.AsKernelFunction()));
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            return kernal;
        });
        return services;
    }
}
