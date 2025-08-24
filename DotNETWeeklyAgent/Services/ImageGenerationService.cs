using Azure;
using Azure.AI.OpenAI;

using DotNETWeeklyAgent.Options;

using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

using OpenAI.Images;

using System.ComponentModel;

namespace DotNETWeeklyAgent.Services;

public class ImageGenerationService
{
    private readonly TinyPNGCompressService _tinyPNGCompressService;

    private readonly AzureOpenAIOptions _azureOptionAIOptions;

    private readonly ILogger<ImageGenerationService> _logger;

    public ImageGenerationService(
        TinyPNGCompressService tinyPNGCompressService,
        IOptions<AzureOpenAIOptions> azureOpenAIOptionsAccessor,
        ILogger<ImageGenerationService> logger
        )
    {
        _tinyPNGCompressService = tinyPNGCompressService;
        _azureOptionAIOptions = azureOpenAIOptionsAccessor.Value;
        _logger = logger;
    }

    [KernelFunction("generate_image")]
    [Description("Generate image from description and issue_number")]
    public async Task<string> GenerateImageAsync(string description, string issue_number)
    {
        AzureOpenAIClient azureClient = new AzureOpenAIClient(
            new Uri(_azureOptionAIOptions.Endpoint),
            new AzureKeyCredential(_azureOptionAIOptions.APIKey));
        var imageClient = azureClient.GetImageClient(_azureOptionAIOptions.ImageDeploymentName);
        ImageGenerationOptions options = new ImageGenerationOptions
        {
            Size = GeneratedImageSize.Auto,
            Quality = "low",
            OutputCompressionFactor = 100,
        };
        _logger.LogInformation("Generating image");
        GeneratedImage image = await imageClient.GenerateImageAsync(description, options);
        string path = $"{Path.GetTempPath()}/issue-{issue_number}-original.png";
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        await File.WriteAllBytesAsync(path, image.ImageBytes.ToArray());
        _logger.LogInformation("Compressing the image");
        string compressPath = $"{Path.GetTempPath()}/issue-{issue_number}.png";
        if (Directory.Exists(compressPath))
        {
            File.Delete(compressPath);
        }
        await _tinyPNGCompressService.CompressAsync(path, compressPath);
        return compressPath;
    }
}
