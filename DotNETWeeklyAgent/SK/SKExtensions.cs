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
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
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
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var githubAPI = new GithubAPIService(httpClientFactory, sp.GetRequiredService<ILogger<GithubAPIService>>());
            // Add github mcp server
            var githubOptions = sp.GetRequiredService<IOptions<GithubOptions>>().Value;
            var sseClientTransportOptions = new SseClientTransportOptions
            {
                Name = "GitHub",
                Endpoint = new Uri(githubOptions.MCPUrl),
                TransportMode = HttpTransportMode.StreamableHttp,
                AdditionalHeaders = new Dictionary<string, string>()
                {
                    { "Authorization", $"Bearer {githubOptions.PAT}" }
                }
            };
            McpClientOptions githubClientOptions = new McpClientOptions
            {
                ClientInfo = new Implementation { Name = "Github Client", Version = "1.0.0" }
            };
            IClientTransport clientTransport = new SseClientTransport(sseClientTransportOptions);
            IMcpClient githubMcpClient = McpClientFactory.CreateAsync(clientTransport, githubClientOptions).ConfigureAwait(false).GetAwaiter().GetResult();
            var githubTools = githubMcpClient.ListToolsAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var filteredGithubTools = FilterMilestoneTools(githubTools).ToList();
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            kernal.Plugins.AddFromFunctions("githubTools", filteredGithubTools.Select(tool => tool.AsKernelFunction()));
            kernal.Plugins.AddFromObject(githubAPI);
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            return kernal;
        });
        return services;
    }

    private static List<McpClientTool> FilterMilestoneTools(IList<McpClientTool> tools)
    {
        return tools.Where(tool => tool.Name.Contains("create_branch", StringComparison.OrdinalIgnoreCase) ||
                                   tool.Name.Contains("push_files", StringComparison.OrdinalIgnoreCase) ||
                                   tool.Name.Contains("create_or_update_file", StringComparison.OrdinalIgnoreCase) ||
                                   tool.Name.Contains("create_pull_request", StringComparison.OrdinalIgnoreCase) ||
                                   tool.Name.Contains("create_pull_request_with_copilot ", StringComparison.OrdinalIgnoreCase) ||
                                   tool.Name.Contains("get_file_contents", StringComparison.OrdinalIgnoreCase)
                          ).ToList();
    }
}
