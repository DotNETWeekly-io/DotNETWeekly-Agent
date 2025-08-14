using Azure.Identity;

using DotNETWeeklyAgent.MCPs;
using DotNETWeeklyAgent.Options;
using DotNETWeeklyAgent.Services;

using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

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
            var kernal = kernalBuilder.Build();
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
            return kernal;
        });
        return services;
    }
}
