namespace DotNETWeeklyAgent.Services;

public interface IBackgroundTaskQueue<T>
{
    ValueTask QueueAsync(T workItem);

    ValueTask<T> DequeueAsync(CancellationToken token);
}
