using System.Threading.Channels;
using backend.Services.Interfaces;

namespace backend.Services.ServiceDef;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<Task>> _queue;

    public BackgroundTaskQueue() => _queue = Channel.CreateUnbounded<Func<Task>>();

    public void Enqueue(Func<Task> workItem)
    {
        if (workItem == null) throw new ArgumentNullException(nameof(workItem));
        _queue.Writer.TryWrite(workItem);
    }

    public async Task<Func<Task>> DequeueAsync(CancellationToken cancellationToken)
        => await _queue.Reader.ReadAsync(cancellationToken);
}
