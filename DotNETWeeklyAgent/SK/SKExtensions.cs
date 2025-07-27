using DotNETWeeklyAgent.Options;
using DotNETWeeklyAgent.Services;

using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace DotNETWeeklyAgent.SK;

public static class SKExtensions
{
    public static IServiceCollection AddSemanticKernal(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var azureOpenAIOptions = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;
            var kernalBuilder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(azureOpenAIOptions.DeploymentName, azureOpenAIOptions.Endpoint, azureOpenAIOptions.APIKey, modelId:azureOpenAIOptions.ModelId);
            kernalBuilder.Services.AddLogging(service => service.AddDebug().SetMinimumLevel(LogLevel.Debug));
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var githubAPI = new GithubAPIService(httpClientFactory);
            var webContent = new WebContentService(httpClientFactory);
            var youtubescript = new YoutubeTranscriptService();
            // Add github mcp server
            /*
            var githubOptions = sp.GetRequiredService<IOptions<GithubOptions>>().Value;
            var sseClientTransportOptions = new SseClientTransportOptions
            {
                Name = "GitHub",
                Endpoint = new Uri(githubOptions.MCPUrl),
                TransportMode = HttpTransportMode.AutoDetect,
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
            */

            // Add FireCrawl MCP Server
            /*
            var fireCrawlOptions = sp.GetRequiredService<IOptions<FireCrawlOptions>>().Value;
            var fireCrawlTransportOoptions = new StdioClientTransportOptions
            {
                Name = "FireCrawl",
                Command = "npx",
                Arguments = ["-y", "firecrawl-mcp"],
                EnvironmentVariables = new Dictionary<string, string>()
                {
                    {"FIRECRAWL_API_KEY", fireCrawlOptions.APIKey ! }
                },
            };
            IMcpClient firecrawMcpClient = McpClientFactory.CreateAsync(new StdioClientTransport(fireCrawlTransportOoptions)).ConfigureAwait(false).GetAwaiter().GetResult();
            var firecrawlTools = firecrawMcpClient.ListToolsAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            */
            var kernal = kernalBuilder.Build();
            
#pragma warning disable SKEXP0001
            kernal.Plugins.AddFromObject(webContent);
            kernal.Plugins.AddFromObject(githubAPI);
            kernal.Plugins.AddFromObject(youtubescript);
#pragma warning restore SKEXP0001
            return kernal;
        });
        return services;
    }
}
