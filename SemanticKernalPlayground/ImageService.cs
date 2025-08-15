using Azure;
using Azure.AI.OpenAI;

using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

using OpenAI.Images;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

using System.ComponentModel;

using TinyPng;

namespace SemanticKernalPlayground;

public class ImageService
{
    private readonly AzureOpenAIOptions _azureOpenAIOptions;

    public ImageService(IOptions<AzureOpenAIOptions> azureOpenAIOptions)
    {
        _azureOpenAIOptions = azureOpenAIOptions.Value;
    }

    [KernelFunction("generate_image")]
    [Description("Get a base64 encode image with png format from description")]
    public async Task<string> RunAsync(string description)
    {
        AzureOpenAIClient azureClient = new AzureOpenAIClient(
            new Uri(_azureOpenAIOptions.Endpoint),
            new AzureKeyCredential(_azureOpenAIOptions.APIKey));
        var client = azureClient.GetImageClient("gpt-image-1");

#pragma warning disable OPENAI001 
        ImageGenerationOptions options = new ImageGenerationOptions()
        {
            Size = GeneratedImageSize.Auto,
            Quality = "low",
            OutputCompressionFactor = 100,
        };
#pragma warning restore OPENAI001

        GeneratedImage image = await client.GenerateImageAsync(description, options);
        string path = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/genimage{DateTimeOffset.Now.Ticks}.png";
        File.WriteAllBytes(path, image.ImageBytes.ToArray());


        string compressPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/genimage{DateTimeOffset.Now.Ticks}.png";
        var png = new TinyPngClient("");
        await png.Compress(path)
            .Download()
            .SaveImageToDisk(compressPath);

        return compressPath;
    }
}
