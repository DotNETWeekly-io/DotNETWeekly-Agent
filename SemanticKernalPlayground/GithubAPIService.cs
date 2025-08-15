using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

using System.ComponentModel;
using System.Text;
using System.Text.Json;
namespace SemanticKernalPlayground;

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
            body = body,
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

    [KernelFunction("create_pull_request_to_add_image")]
    [Description("Create a pull request to add an image to a github repo")]
    public async Task CreatePullRequestToAddImage(string owner, string repo, string imageFilePath)
    {
        var client = _httpClientFactory.CreateClient("GithubAPI");
        _logger.LogInformation("Getting the latest commit SHA for {owner}/{repo}", owner, repo);
        var path = $"https://api.github.com/repos/{owner}/{repo}/git/ref/heads/master";
        var response = await client.GetStringAsync(path);
        using var doc = JsonDocument.Parse(response);
        var commitSha = doc.RootElement.GetProperty("object").GetProperty("sha").GetString();

        _logger.LogInformation("Creating a new branch");
        var branch = $"image-{Guid.NewGuid()}";
        var createRef = new
        {
            @ref = $"refs/heads/{branch}",
            sha = commitSha
        };

        var responseMessage = await client.PostAsync(
            $"https://api.github.com/repos/{owner}/{repo}/git/refs",
            new StringContent(JsonSerializer.Serialize(createRef), Encoding.UTF8, "application/json")
            );
        // responseMessage.EnsureSuccessStatusCode();
        var body = await responseMessage.Content.ReadAsStringAsync();


        _logger.LogInformation("Creating image file");
        var imageFileName = Path.GetFileName(imageFilePath);
        var base64Image = Convert.ToBase64String(System.IO.File.ReadAllBytes(imageFilePath));
        var putFile = new
        {
            message = $"Add {imageFileName}",
            content = base64Image,
            branch = branch,
            committer = new { name = "gaufung", email = "gaufung@outlook.com" }
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
            body = $"This PR adds the image {imageFileName} to the repository."
        };

        responseMessage =  await client.PostAsync(
            $"https://api.github.com/repos/{owner}/{repo}/pulls",
            new StringContent(JsonSerializer.Serialize(pr), Encoding.UTF8, "application/json")
            );
        responseMessage.EnsureSuccessStatusCode();
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