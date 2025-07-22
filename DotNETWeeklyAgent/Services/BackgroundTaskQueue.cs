
using System.Threading.Channels;

namespace DotNETWeeklyAgent.Services;

public class BackgroundTaskQueue<T> : IBackgroundTaskQueue<T>
{
    private readonly Channel<T> _queue;

    public BackgroundTaskQueue()
    {
        var options = new UnboundedChannelOptions()
        {
            SingleReader = true,
            SingleWriter = false
        };
        _queue = Channel.CreateUnbounded<T>(options);
    }

    public ValueTask<T> DequeueAsync(CancellationToken token)
    {
        return _queue.Reader.ReadAsync(token);
    }

    public ValueTask QueueAsync(T workItem)
    {
        return _queue.Writer.WriteAsync(workItem);
    }
}
