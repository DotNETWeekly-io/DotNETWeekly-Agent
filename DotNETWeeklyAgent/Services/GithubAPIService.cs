using DotNETWeeklyAgent.Models;

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

        private static int prefixLength = "【开源项目】".Length;

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

        [KernelFunction("get_episode_content")]
        [Description("Get the content of an episode by github owner, repo and episode number")]
        public async Task<string> GetEpisodeContent(string owner, string repo, int number)
        {
            var client = _httpClientFactory.CreateClient("GithubAPI");
            var path = $"repos/{owner}/{repo}/issues";
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var openIssues = JsonSerializer.Deserialize<List<OpenIssue>>(content);
            if (openIssues == null || openIssues.Count == 0)
            {
                return $"Empty open issue from {owner}/{repo}, stop proceeding";
            }

            var tasks = openIssues
                .Select(async issue =>
                {
                    string path = $"repos/{owner}/{repo}/issues/{issue.Number}/comments";
                    using var request = new HttpRequestMessage(HttpMethod.Get, path);
                    var contentResponse = await client.SendAsync(request);
                    contentResponse.EnsureSuccessStatusCode();
                    var commentsContent = await contentResponse.Content.ReadAsStringAsync();
                    var comments = JsonSerializer.Deserialize<List<IssueComment>>(commentsContent);
                    if (comments != null && comments.Count > 0)
                    {
                        issue.Content = comments.First().Comment;
                    }
                });

            await Task.WhenAll(tasks);
            var validate = openIssues.FirstOrDefault(issue => string.IsNullOrWhiteSpace(issue.Content));
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($".NET 每周分享第 {number} 期");
            sb.AppendLine(CreateEpisodeSection(openIssues.Where(issue => issue.IssueCategory == IssueCategory.News).ToList(), "行业资讯"));
            sb.AppendLine(CreateEpisodeSection(openIssues.Where(issue => issue.IssueCategory == IssueCategory.Article).ToList(), "文章推荐"));
            sb.AppendLine(CreateEpisodeSection(openIssues.Where(issue => issue.IssueCategory == IssueCategory.Video).ToList(), "视频推荐"));
            sb.AppendLine(CreateEpisodeSection(openIssues.Where(issue => issue.IssueCategory == IssueCategory.OSS).ToList(), "开源项目"));
            return sb.ToString();
        }


        private static string CreateEpisodeSection(List<OpenIssue> issues, string sectionName)
        {
            var sb = new StringBuilder();
            if (issues == null || issues.Count == 0)
            {
                return Environment.NewLine;
            }

            sb.AppendLine($"## {sectionName}");
            for (int i = 0; i < issues.Count; i++)
            {
                var issue = issues[i];
                sb.AppendLine($"{i + 1}、 [{issue.Title.Substring(prefixLength)}]({issue.Link})");
                sb.AppendLine($"{issue.Content}");
            }
            return sb.ToString();
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
