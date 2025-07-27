using Brackets;

using Microsoft.SemanticKernel;

using Readability;

using System.ComponentModel;

namespace DotNETWeeklyAgent.Services;

public class WebContentService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WebContentService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [KernelFunction("get_web_link_content")]
    [Description("get web content by the web link. It's designed for aritcle and news github issue.")]
    public async Task<string> GetWebContent(string link)
    {
        var httpClient = _httpClientFactory.CreateClient("WebContent");
        httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Add("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
        var documentContentStream = await httpClient.GetStreamAsync(link);
        var document = await Document.Html.ParseAsync(documentContentStream);
        var doc = document.ParseArticle();
        return doc.ToString();
    }
}
