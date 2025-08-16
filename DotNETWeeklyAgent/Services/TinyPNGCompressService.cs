using DotNETWeeklyAgent.Options;

using Microsoft.Extensions.Options;

using TinyPng;

namespace DotNETWeeklyAgent.Services;

public class TinyPNGCompressService
{
    private readonly TinyPNGOptions _tinyPNGOptions;

    public TinyPNGCompressService(IOptions<TinyPNGOptions> tinyPNGOptionAccessor)
    {
        _tinyPNGOptions = tinyPNGOptionAccessor.Value;
    }

    public async Task CompressAsync(string pathToFile, string newPathToFile)
    {
        var png = new TinyPngClient(_tinyPNGOptions.APIKey);
        await png.Compress(pathToFile)
            .Download()
            .SaveImageToDisk(newPathToFile);
    }
}
