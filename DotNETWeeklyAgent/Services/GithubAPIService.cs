using Microsoft.SemanticKernel;

using System.ComponentModel;
using System.Text;
using System.Text.Json;

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
            var client = _httpClientFactory.CreateClient("GithubAPI");
            var path = $"repos/{owner}/{repo}/issues/{issue_number}/comments";
            var payload = new
            {
                body=body,
            };
            using var request = CreateGithubRequestMessage(path, JsonSerializer.Serialize(payload));
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
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
