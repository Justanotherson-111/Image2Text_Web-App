namespace backend.Services.Interfaces;

public interface IBackgroundTaskQueue
{
    void Enqueue(Func<Task> workItem);
    Task<Func<Task>> DequeueAsync(CancellationToken cancellationToken);
}
