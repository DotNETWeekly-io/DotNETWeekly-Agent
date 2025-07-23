using DotNETWeeklyAgent.Options;

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
            var kernalBuilder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(
                azureOpenAIOptions.ModelId, azureOpenAIOptions.Endpoint, azureOpenAIOptions.APIKey);
            // Add github mcp server
            var githubOptions = sp.GetRequiredService<IOptions<GithubOptions>>().Value;
            var sseClientTransportOptions = new SseClientTransportOptions
            {
                Name = "GitHub",
                Endpoint = new Uri(githubOptions.Url),
                TransportMode = HttpTransportMode.StreamableHttp,
                AdditionalHeaders = new Dictionary<string, string>()
                {
                    { "Authorization", $"Bearer {githubOptions.PAT}" }
                }
            };
            McpClientOptions clientOptions = new McpClientOptions
            {
                ClientInfo = new Implementation { Name = "Tool client", Version = "1.0.0" }
            };
            IClientTransport clientTransport = new SseClientTransport(sseClientTransportOptions);
            IMcpClient mcpClient = McpClientFactory.CreateAsync(clientTransport, clientOptions).ConfigureAwait(false).GetAwaiter().GetResult();
            var tools = mcpClient.ListToolsAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var kernal = kernalBuilder.Build();
#pragma warning disable SKEXP0001
            kernal.Plugins.AddFromFunctions("Github", tools.Select(tool => tool.AsKernelFunction()));
#pragma warning restore SKEXP0001
            return kernal;
        });
        return services;
    }
}
