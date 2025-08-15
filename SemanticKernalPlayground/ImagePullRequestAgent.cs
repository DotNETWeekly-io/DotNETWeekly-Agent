using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernalPlayground;

public class ImagePullRequestAgent
{
    private readonly AzureOpenAIOptions _azureOpenAIOptions;

    private readonly GithubOptions _githubOptions;

    private readonly ILoggerFactory _loggerFactory;

    private readonly ImageService _imageService;

    private readonly GithubAPIService _githubAPIService;


    public ImagePullRequestAgent(
        IOptions<AzureOpenAIOptions> azureOpenAIOptionsAccessor,
        IOptions<GithubOptions> githubOptionsAccessor,
        ILoggerFactory loggerFactory,
        ImageService imageService,
        GithubAPIService githubApiService
        )
    {
        _azureOpenAIOptions = azureOpenAIOptionsAccessor.Value;
        _githubOptions = githubOptionsAccessor.Value;
        _loggerFactory = loggerFactory;
        _imageService = imageService;
        _githubAPIService = githubApiService;
    }

    public async Task RunAsync()
    {
        IKernelBuilder builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<ILoggerFactory>(_loggerFactory);
        builder.AddAzureOpenAIChatCompletion(_azureOpenAIOptions.DeploymentName, _azureOpenAIOptions.Endpoint, _azureOpenAIOptions.APIKey, modelId: _azureOpenAIOptions.ModelId);
        var kernel = builder.Build();

        ChatCompletionAgent imageGenerateAgent = CreateImageGenerateAgent(kernel);

        var issueComment = """
            微软宣布 .NET Conf 2025 正式开启议题征集，截止日期为 2025 年 8 月 31 日 23:59 PDT。大会将于 11 月 11–13 日在线举行，重点发布 .NET 10，并深入探讨 .NET Aspire 新特性及 AI 场景。官方期望社区提交 30 分钟（含 Q&A）的分享，每位讲者限 1 场，可按所在时区远程呈现。

            优选内容：
            - Web：ASP.NET Core、Blazor 等实战
            - 移动/桌面：.NET MAUI、遗留系统现代化
            - AI/ML：ML.NET、AI 集成案例
            - IoT/Edge：嵌入式与设备端方案
            - 游戏：Unity 或原生 .NET 游戏开发
            - 云与容器：微服务、云原生实践
            - DevOps：高效 CI/CD、部署策略
            - 开源：库/工具分享或贡献体验

            评审更看重真实项目经验、架构剖析与生产最佳实践，而非简单功能介绍。建议在提案中附上过往演讲或项目演示视频，帮助评委了解讲者风格。征集入口已开放（sessionize.com/net-conf-2025），大会免费、面向全球开发者，鼓励首次登台的新人投稿，与 .NET 社区共同庆祝年度盛会。
            """;

        var message = $"""
            can you generate an image for the following description and create a pull request to the GitHub repository?
            owner: DotNETWeekly-io
            repo: DotNETWeekly
            description: {issueComment}
            """;
        var result = await imageGenerateAgent.InvokeAsync(message).FirstAsync();
    }


    private ChatCompletionAgent CreateImageGenerateAgent(Kernel kernel)
    {
        var cloneKernel = kernel.Clone();
        cloneKernel.Plugins.AddFromObject(_imageService);
        cloneKernel.Plugins.AddFromObject(_githubAPIService);

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        };

        ChatCompletionAgent imageGenerateAgent = new ChatCompletionAgent()
        {
            Name = "ImageGenerateAgent",
            Instructions = """
            You are an image generation agent. Given a description, you will generate an image based on the description and create a pull request to the GitHub repository with the image.

            <Input>
            The input contains the following properties:
            - gitub owner: The owner of the GitHub repository.
            - gitub repo: The name of the GitHub repository.
            - description: The description of the image to be generated.
            </Input>

            <Global>
            Your job is to create a image based on the description and then create a pull request to the GitHub repository with the image.
            </Global>

            <StepsToFollow>
            1. Generate an image based on the description and save it in the disk with file path. If the description is Chinese, you should translate it to English first. If the description is too long, you should summary it to 100 characters
            2. Create a pull request to the GitHub repository with the image file.
            </StepsToFollow>
            """,
            Description = "An agent to create a image base on the description and create pull request",
            Kernel = cloneKernel,
            Arguments = new KernelArguments(openAIPromptExecutionSettings),
        };

        return imageGenerateAgent;
    }
}
