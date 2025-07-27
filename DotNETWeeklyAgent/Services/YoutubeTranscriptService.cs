using CliWrap;

using Microsoft.SemanticKernel;

using System.ComponentModel;
using System.Text;
using System.Web;

namespace DotNETWeeklyAgent.Services;

public sealed class YoutubeTranscriptService
{
    private readonly Logger<YoutubeTranscriptService> _logger;
    public YoutubeTranscriptService(Logger<YoutubeTranscriptService> logger)
    {
        _logger = logger;
    }


    [KernelFunction("get_youtube_video_transsript")]
    [Description("get the youtube video transcript with youtube video link")]
    public async Task<string> GetYoutbeTranscript(string link)
    {
        Uri uri = new Uri(link);
        var queryParams = HttpUtility.ParseQueryString(uri.Query);
        if (queryParams == null)
        {
            _logger.LogError("The youtube link is not valid: {link}", link);
            return "The youtube video is not valid. Stop proceeding";
        }

        string id = queryParams["v"];

        if (!string.IsNullOrWhiteSpace(id))
        {
            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();
            var result = await Cli.Wrap("./Scripts/youtube_transcript.exe")
                .WithArguments([id])
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .ExecuteAsync();

            if (result.IsSuccess)
            {
                return stdOutBuffer.ToString();
            }
            else
            {
                _logger.LogError("Failed to get the youtube video transcript with error: {error}", stdErrBuffer.ToString()); 
                return $"Unable to get youtube video transcript with error {stdErrBuffer.ToString()}. Stop proceeding";
            }
        }
        else
        {
            _logger.LogError("The youtube link doesn't have video id: {link}", link);
            return "The youtube link doesn't have video id. Stop proceeding";
        }
        

    }
}
