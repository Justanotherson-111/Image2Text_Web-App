using Microsoft.AspNetCore.SignalR;

namespace backend.Hubs
{
    public class OcrHub : Hub
    {
        public async Task JoinImageRoom(string imageId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, imageId);
        }

        public async Task LeaveImageRoom(string imageId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, imageId);
        }
    }
}
