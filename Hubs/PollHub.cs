using Microsoft.AspNetCore.SignalR;

namespace _1_the_real_time_polling_app_focus_signalr_websockets.Hubs;

public class PollHub : Hub
{
    public async Task JoinRoom(string roomCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
    }
}
