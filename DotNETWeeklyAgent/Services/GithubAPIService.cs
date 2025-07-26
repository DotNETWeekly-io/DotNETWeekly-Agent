using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DotNETWeeklyAgent.Services
{
    public class GithubAPIService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GithubAPIService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [KernelFunction("add_github_issue_comment")]
        [Description("Add comment to the github issue by ower, repo, issue_number and body")]
        public async Task AddIssueComment(string owner, string repo, int issue_number, string body)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://api.github.com/repos/{owner}/{repo}/issues/{issue_number}/comments";
            var payload = JsonSerializer.Serialize(new { body });
            using var request = CreateGithubRequestMessage(url, payload);

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            // Optionally handle response content if needed
        }

        private HttpRequestMessage CreateGithubRequestMessage(string url, string jsonPayload)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("User-Agent", "DotNETWeeklyAgent");
            // Add authentication header if needed, e.g.:
            // request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "<token>");
            return request;
        }
    }
}
