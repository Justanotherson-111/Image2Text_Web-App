namespace backend.Services.Interfaces
{
    public interface IRateLimiterService
    {
        bool CanPerformAction(Guid userId, string action);
        void RecordAction(Guid userId, string action);
    }
}