using Brackets;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

using Readability;

using System.ComponentModel;

namespace SemanticKernalPlayground;

public class WebContentService
{
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ILogger<WebContentService> _logger;

    public WebContentService(IHttpClientFactory httpClientFactory, ILogger<WebContentService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [KernelFunction("get_web_link_content")]
    [Description("get web content by the web link. It's designed for aritcle and news github issue.")]
    public async Task<string> GetWebContent(string link)
    {
        _logger.LogInformation("Getting web content for link: {link}", link);
        var httpClient = _httpClientFactory.CreateClient("WebContent");
        httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Add("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");

        try
        {
            var documentContentStream = await httpClient.GetStreamAsync(link);
            var document = await Document.Html.ParseAsync(documentContentStream);
            var doc = document.ParseArticle();
            return doc.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get the content of {link}, exception: {exception}", link, ex.Message);
            return "Failed to get the website content, stop proceeding.";
        }

    }
}
