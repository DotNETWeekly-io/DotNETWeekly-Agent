using DotNETWeeklyAgent.Options;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace DotNETWeeklyAgent.MCPs;

public class GithubMCP
{
    public static async Task<IMcpClient> Create(GithubOptions githubOptions, ILoggerFactory loggerFactory)
    {
        var sseClientOptions = new SseClientTransportOptions
        {
            Name  = "GithubMCP",
            Endpoint = new Uri(githubOptions.MCPUrl),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {githubOptions.PAT}" }
            }
        };
        McpClientOptions clientOptions = new McpClientOptions
        {
            ClientInfo = new Implementation { Name = "Github client", Version = "1.0.0" }
        };

        IClientTransport clientTransport = new SseClientTransport(sseClientOptions, loggerFactory);
        IMcpClient mcpClient = await McpClientFactory.CreateAsync(
            clientTransport,
            clientOptions,
            loggerFactory
        );

        return mcpClient;
    }
}
