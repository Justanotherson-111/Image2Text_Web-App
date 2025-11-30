using backend.Database;
using backend.Models;
using backend.Services.Interfaces;

namespace backend.Services.ServiceDef;

public class OCRBackgroundService : BackgroundService
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OCRBackgroundService> _logger;

    public OCRBackgroundService(IBackgroundTaskQueue queue, IServiceScopeFactory scopeFactory, ILogger<OCRBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OCR Background Service started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await _queue.DequeueAsync(stoppingToken);
            try { await workItem(); }
            catch (Exception ex) { _logger.LogError(ex, "OCR job failed"); }
        }
    }

    public void EnqueueOcrJob(Func<Task> workItem) => _queue.Enqueue(workItem);
}
