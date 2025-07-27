using Brackets;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using Readability;

using YoutubeTranscriptApi;

//var httpClient = new HttpClient();
//httpClient.DefaultRequestHeaders.Add("User-Agent",
//    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36");

//httpClient.DefaultRequestHeaders.Add("Accept",
//    "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
//httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
//var documentContentStream = await httpClient.GetStreamAsync("https://blog.jetbrains.com/dotnet/2025/07/15/dotinsights-july-2025/");
//var document = await Document.Html.ParseAsync(documentContentStream);
//var doc = document.ParseArticle();
//var str = doc.ToString();

YouTubeTranscriptApi api = new YouTubeTranscriptApi();
var scripts = api.ListTranscripts("DpyjAKmNwpI").ToList();

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
#if DEBUG
    .AddJsonFile("appsettings.Development.json")
#endif
    .Build();

var builder = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(
    deploymentName: config["AzureOpenAI:DeploymentName"],
    endpoint: config["AzureOpenAI:Endpoint"],
    apiKey: config["AzureOpenAI:APIKey"],
    modelId: config["AzureOpenAI:ModelId"]);

builder.Services.AddLogging(service => service.AddConsole().SetMinimumLevel(LogLevel.Debug));

Kernel kernel = builder.Build();
// add github mcp server

var sseClientTransportOptions = new SseClientTransportOptions
{
    Name = "github",
    Endpoint = new Uri(config["Github:Url"]),
    TransportMode = HttpTransportMode.AutoDetect,
    AdditionalHeaders = new Dictionary<string, string?>()
    {
        { "Authorization", $"Bearer {config["Github:PAT"]}" }
    },
};

McpClientOptions githubClientOptions = new McpClientOptions
{
    ClientInfo = new Implementation { Name = "Github Client", Version = "1.0.0" }
};

IClientTransport clientTransport = new SseClientTransport(sseClientTransportOptions);
IMcpClient githubMcpClient = McpClientFactory.CreateAsync(clientTransport,
    githubClientOptions).ConfigureAwait(false).GetAwaiter().GetResult();
var githubTools = githubMcpClient.ListToolsAsync().ConfigureAwait(false).GetAwaiter().GetResult();

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
kernel.Plugins.AddFromFunctions("githubTools", githubTools.Select(p => p.AsKernelFunction()));
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

var history = new ChatHistory();

string? userInput;
do
{
    // Collect user input
    Console.Write("User > ");
    userInput = Console.ReadLine();

    // Add user input
    history.AddUserMessage(userInput);

    // Get the response from the AI
    var result = await chatCompletionService.GetChatMessageContentAsync(
        history,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel);

    // Print the results
    Console.WriteLine("Assistant > " + result);

    // Add the message from the agent to the chat history
    history.AddMessage(result.Role, result.Content ?? string.Empty);
} while (userInput is not null);