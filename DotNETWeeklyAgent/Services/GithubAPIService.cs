using Microsoft.SemanticKernel;

using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace DotNETWeeklyAgent.Services
{
    public class GithubAPIService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly ILogger<GithubAPIService> _logger;

        public GithubAPIService(IHttpClientFactory httpClientFactory, ILogger<GithubAPIService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [KernelFunction("add_github_issue_comment")]
        [Description("Add comment to the github issue by ower, repo, issue_number and body")]
        public async Task AddIssueComment(string owner, string repo, int issue_number, string body)
        {
            var client = _httpClientFactory.CreateClient("GithubAPI");
            var path = $"repos/{owner}/{repo}/issues/{issue_number}/comments";
            var payload = new
            {
                body=body,
            };

            try
            {
                using var request = CreateGithubRequestMessage(path, JsonSerializer.Serialize(payload));
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add comment to the issue {owner}/{repo}#{issue_number}", owner, repo, issue_number);
            }
        }

        private HttpRequestMessage CreateGithubRequestMessage(string path, string jsonPayload)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            return request;
        }
    }
}
