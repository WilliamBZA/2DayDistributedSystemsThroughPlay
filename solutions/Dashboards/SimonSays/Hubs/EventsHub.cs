using Microsoft.AspNetCore.SignalR;

namespace SimonSays.Hubs;

public class EventsHub : Hub
{
    public override Task OnConnectedAsync()
    {
        return base.OnConnectedAsync();
    }

    public async Task BroadcastEvent(string eventType, DateTime timestamp, string payload)
    {
        await Clients.All.SendAsync("ReceiveEvent", eventType, timestamp, payload);
    }
}