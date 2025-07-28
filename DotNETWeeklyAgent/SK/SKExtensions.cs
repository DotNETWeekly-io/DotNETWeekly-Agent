using Azure.Identity;

using DotNETWeeklyAgent.Options;
using DotNETWeeklyAgent.Services;

using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace DotNETWeeklyAgent.SK;

public static class SKExtensions
{
    public static IServiceCollection AddSemanticKernal(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
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
}
