using DotNETWeeklyAgent.Options;

using Microsoft.Extensions.Options;

using System.Net.Http.Headers;

namespace DotNETWeeklyAgent.Extensions;

public static class HttpClientExtensions
{
    public static IServiceCollection AddGithubAPIHttpClient(this IServiceCollection services)
    {
        services.AddHttpClient("GithubAPI")
            .ConfigureHttpClient((sp, client) =>
            {
                var githubOptions = sp.GetRequiredService<IOptions<GithubOptions>>().Value;
                client.BaseAddress = new Uri(githubOptions.APIUrl);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubOptions.PAT);
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
                client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
                client.DefaultRequestHeaders.Add("User-Agent", "DotNETWeekly-Agent");
            });

        return services;
    }

    public static IServiceCollection AddWebContentHttpClient(this IServiceCollection services)
    {
        services.AddHttpClient("WebContent")
            .ConfigureHttpClient(httpClient =>
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36");
                httpClient.DefaultRequestHeaders.Add("Accept",
                    "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
            });
        return services;
    }
}
