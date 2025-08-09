using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace SemanticKernalPlayground;

public class SingleAgent
{
    private readonly AzureOpenAIOptions _azureOpenAIOptions;

    private readonly GithubOptions _githubOptions;

    private readonly ILoggerFactory _loggerFactory;

    public SingleAgent(IOptions<AzureOpenAIOptions> azureOpenAIOptionsAccessor,
        IOptions<GithubOptions> githubOptionsAccessor,
        ILoggerFactory loggerFactory)
    {
        _azureOpenAIOptions = azureOpenAIOptionsAccessor.Value;
        _githubOptions = githubOptionsAccessor.Value;
        _loggerFactory = loggerFactory;
    }

    public async Task RunAsync()
    {
        IKernelBuilder builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<ILoggerFactory>(_loggerFactory);
        builder.AddAzureOpenAIChatCompletion(_azureOpenAIOptions.DeploymentName, _azureOpenAIOptions.Endpoint, _azureOpenAIOptions.APIKey, modelId: _azureOpenAIOptions.ModelId);

        var kernel = builder.Build();
        var sseClientTransportOptions = new SseClientTransportOptions
        {
            Name = "GitHub",
            Endpoint = new Uri(_githubOptions.MCPUrl),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string>()
                {
                    { "Authorization", $"Bearer {_githubOptions.PAT}" }
                }
        };
        McpClientOptions githubClientOptions = new McpClientOptions
        {
            ClientInfo = new Implementation { Name = "Github Client", Version = "1.0.0" }
        };
        IClientTransport clientTransport = new SseClientTransport(sseClientTransportOptions);
        IMcpClient githubMcpClient = McpClientFactory.CreateAsync(clientTransport, githubClientOptions).ConfigureAwait(false).GetAwaiter().GetResult();
        var githubTools = githubMcpClient.ListToolsAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        var filteredGithubTools = githubTools.ToList();
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        kernel.Plugins.AddFromFunctions("githubTools", filteredGithubTools.Select(tool => tool.AsKernelFunction()));
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        };

        // Create the agent
        ChatCompletionAgent agent =
            new()
            {
                Name = "SummarizationAgent",
                Instructions = "Talk to the user",
                Kernel = kernel,
                Arguments = new KernelArguments(openAIPromptExecutionSettings)
            };


        await foreach (ChatMessageContent response in agent.InvokeAsync(new ChatMessageContent(AuthorRole.User, "What're the open issues in DotNETWeekly-io/DotNETWeekly repo?")))
        {
            Console.WriteLine(response.Content);
        }
    }
}
