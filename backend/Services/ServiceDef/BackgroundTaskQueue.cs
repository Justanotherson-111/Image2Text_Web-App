using System.Threading.Channels;
using backend.Services.Interfaces;

namespace backend.Services.ServiceDef;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<Task>> _queue;

    public BackgroundTaskQueue()
    {
        _queue = Channel.CreateBounded<Func<Task>>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public async Task Enqueue(Func<Task> workItem)
    {
        if (workItem == null) throw new ArgumentNullException(nameof(workItem));
        await _queue.Writer.WriteAsync(workItem);
    }

    public async Task<Func<Task>> DequeueAsync(CancellationToken cancellationToken)
        => await _queue.Reader.ReadAsync(cancellationToken);
}

