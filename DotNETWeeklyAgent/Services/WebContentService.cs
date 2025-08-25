using Microsoft.SemanticKernel;

using System.ComponentModel;
using System.Diagnostics;

namespace DotNETWeeklyAgent.Services;

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
    public string GetWebContent(string link)
    {
        _logger.LogInformation("Getting web content for link: {link}", link);

        try
        {
            string exePath = "./Scripts/webcontent_transcript.exe";
            string arguments = link;
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Failed to get the web content with error: {error}", error);
                return $"Unable to get web content with error {error}. Stop proceeding";
            }
            else
            {
                return output;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get the content of {link}, exception: {exception}", link, ex.Message);
            return "Failed to get the website content, stop proceeding.";
        }
        
    }
}
