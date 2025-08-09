using Microsoft.Extensions.DependencyInjection;

namespace SemanticKernalPlayground;

public static class Extensions
{
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
