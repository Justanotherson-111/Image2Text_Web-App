namespace backend.Services.Interfaces;

public interface IBackgroundTaskQueue
{
    Task Enqueue(Func<Task> workItem);
    Task<Func<Task>> DequeueAsync(CancellationToken cancellationToken);
}
