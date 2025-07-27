using CliWrap;

using Microsoft.SemanticKernel;

using System.ComponentModel;
using System.Text;
using System.Web;

namespace DotNETWeeklyAgent.Services;

public sealed class YoutubeTranscriptService
{
    [KernelFunction("get_youtube_video_transsript")]
    [Description("get the youtube video transcript with youtube video link")]
    public async Task<string> GetYoutbeTranscript(string link)
    {
        Uri uri = new Uri(link);
        var queryParams = HttpUtility.ParseQueryString(uri.Query);
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
                return $"Unable to get youtube video transcript with error {stdErrBuffer.ToString()}";
            }
        }
        else
        {
            return "The youtube link doesn't have video id";
        }
        

    }
}
