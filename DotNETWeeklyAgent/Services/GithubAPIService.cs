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

        [KernelFunction("get_issue_link")]
        [Description("Get link from of github issue by owner, repo and issue_number")]
        public async Task<string> GetIssueLink(string owner, string repo, int issue_number)
        {
            var client = _httpClientFactory.CreateClient("GithubAPI");
            var path = $"repos/{owner}/{repo}/issues/{issue_number}";
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            var issue = JsonSerializer.Deserialize<Issue>(content);
            if (issue == null)
            {
                return $"Cannot find the issue.";
            }

            return issue.Body;
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

                    path = "repos/{owner}/{repo}/contents/assets/images/issue-{issue.Number}.png?ref=master";
                    using var imageRequest = new HttpRequestMessage(HttpMethod.Get, path);
                    var response = await client.SendAsync(imageRequest);
                    if (response.IsSuccessStatusCode)
                    {
                        var imageContent = await response.Content.ReadAsStringAsync();
                        var imageData = JsonSerializer.Deserialize<JsonElement>(imageContent);
                        if (imageData.TryGetProperty("download_url", out var downloadUrl))
                        {
                            issue.ImageUrl = downloadUrl.GetString() ?? string.Empty;
                        }
                    }
                });

            await Task.WhenAll(tasks);
            _logger.LogInformation("Get {count} open issues and comments from {owner}/{repo}", openIssues.Count, owner, repo);
            var validate = openIssues.FirstOrDefault(issue => string.IsNullOrWhiteSpace(issue.Content));
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($".NET 每周分享第 {number} 期");
            sb.AppendLine(CreateEpisodeSection(openIssues.Where(issue => issue.IssueCategory == IssueCategory.News).ToList(), "行业资讯"));
            sb.AppendLine(CreateEpisodeSection(openIssues.Where(issue => issue.IssueCategory == IssueCategory.Article).ToList(), "文章推荐"));
            sb.AppendLine(CreateEpisodeSection(openIssues.Where(issue => issue.IssueCategory == IssueCategory.Video).ToList(), "视频推荐"));
            sb.AppendLine(CreateEpisodeSection(openIssues.Where(issue => issue.IssueCategory == IssueCategory.OSS).ToList(), "开源项目"));
            return sb.ToString();
        }

        [KernelFunction("get_issue_comment")]
        [Description("get the issue comment by github ower, repo and issue_number")]
        public async Task<string> GetIssueComment(string owner, string repo, int issue_number)
        {
            var client = _httpClientFactory.CreateClient("GithubAPI");
            var path = $"repos/{owner}/{repo}/issues/{issue_number}/comments";
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var comments = JsonSerializer.Deserialize<List<IssueComment>>(content);
            if (comments != null && comments.Count > 0)
            {
                return comments.First().Comment;
            }
            return string.Empty;
        }

        [KernelFunction("create_pull_request_to_add_image")]
        [Description("Create a github pull request to add an image to this github repo with this issue number")]
        public async Task CreatePullRequestToAddImage(string owner, string repo, int issue_number, string imageFilePath)
        {
            var client = _httpClientFactory.CreateClient("GithubAPI");
            _logger.LogInformation("Getting the latest master commit SHA for {owner}/{repo}", owner, repo);
            var uri = $"https://api.github.com/repos/{owner}/{repo}/git/ref/heads/master";
            var response = await client.GetStringAsync(uri);
            using var doc = JsonDocument.Parse(response);
            var commitSha = doc.RootElement.GetProperty("object").GetProperty("sha").GetString();

            _logger.LogInformation("creating a new branch");
            var branch = $"imge-{Guid.NewGuid()}";
            var createRef = new
            {
                @ref = $"refs/heads/{branch}",
                sha = commitSha
            };
            var responseMessage = await client.PostAsync(
           $"https://api.github.com/repos/{owner}/{repo}/git/refs",
           new StringContent(JsonSerializer.Serialize(createRef), Encoding.UTF8, "application/json")
            );
            responseMessage.EnsureSuccessStatusCode();

            _logger.LogInformation("Uploading the image file.");
            var imageFileName = Path.GetFileName(imageFilePath);
            var base64Image = Convert.ToBase64String(File.ReadAllBytes(imageFilePath));
            var putFile = new
            {
                message = $"Add {imageFileName}",
                content = base64Image,
                branch = branch,
                commiter = new { name = "gaufung", email = "gaufung@outlook.com" }
            };

            responseMessage = await client.PutAsync(
            $"https://api.github.com/repos/{owner}/{repo}/contents/assets/images/{imageFileName}",
            new StringContent(JsonSerializer.Serialize(putFile), Encoding.UTF8, "application/json")
            );
            responseMessage.EnsureSuccessStatusCode();

            _logger.LogInformation("Creating pull request");
            var pr = new
            {
                title = $"Add image {imageFileName}",
                head = branch,
                @base = "master",
                body = $"This PR adds the image {imageFileName} to the repository.",
            };

            responseMessage = await client.PostAsync(
                $"https://api.github.com/repos/{owner}/{repo}/pulls",
                new StringContent(JsonSerializer.Serialize(pr), Encoding.UTF8, "application/json")
                );
            responseMessage.EnsureSuccessStatusCode();
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
                sb.AppendLine();
                if (!string.IsNullOrEmpty(issue.ImageUrl))
                {
                    sb.AppendLine($"![image]({issue.ImageUrl})");
                }
                sb.AppendLine();
                sb.AppendLine($"{issue.Content}");
                sb.AppendLine(Environment.NewLine);
            }
            sb.AppendLine(Environment.NewLine);
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
