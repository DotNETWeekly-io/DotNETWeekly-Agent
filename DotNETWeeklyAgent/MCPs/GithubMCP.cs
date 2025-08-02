using DotNETWeeklyAgent.Options;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace DotNETWeeklyAgent.MCPs;

public class GithubMCP
{
    public static IMcpClient Create(GithubOptions githubOptions, ILoggerFactory loggerFactory)
    {
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

        return githubMcpClient;
    }
}
