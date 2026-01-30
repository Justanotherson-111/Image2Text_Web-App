using System.Collections.Concurrent;
using backend.Services.Interfaces;

namespace backend.Services.ServiceDef
{
    public class RateLimiterService : IRateLimiterService
    {
        private readonly ConcurrentDictionary<string, DateTime> _userActions = new();
        private readonly TimeSpan _actionLimit = TimeSpan.FromMinutes(1);

        public bool CanPerformAction(Guid userId, string action)
        {
            var key = $"{userId}:{action}";
            if (_userActions.TryGetValue(key, out var lastAction))
                return (DateTime.UtcNow - lastAction) > _actionLimit;

            return true;
        }
        public void RecordAction(Guid userId, string action)
        {
            var key = $"{userId}:{action}";
            _userActions[key] = DateTime.UtcNow;
        }
    }
}
