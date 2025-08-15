using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using SemanticKernalPlayground;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
#if DEBUG
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
#endif
    .Build();


IServiceCollection services = new ServiceCollection();
services.Configure<AzureOpenAIOptions>(config.GetSection("AzureOpenAI"));
services.Configure<GithubOptions>(config.GetSection("Github"));
services.AddLogging(builder => builder.AddConsole());
services.AddWebContentHttpClient();
services.AddGithubAPIHttpClient();
services.AddSingleton<GithubAPIService>();
services.AddSingleton<WebContentService>();
services.AddSingleton<SingleAgent>();
services.AddSingleton<IssueMultiAgents>();
services.AddSingleton<EpisodeMultiAgents>();
services.AddSingleton<ImageService>();
services.AddSingleton<ImagePullRequestAgent>();



var sp = services.BuildServiceProvider();
var agent = sp.GetRequiredService<ImagePullRequestAgent>();

await agent.RunAsync();