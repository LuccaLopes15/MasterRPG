using Microsoft.AspNetCore.SignalR;

namespace T20Master.Web.Hubs;

public class CampaignSessionHub : Hub
{
    public async Task JoinSession(Guid campaignId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"campaign:{campaignId}");
    }

    public async Task LeaveSession(Guid campaignId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"campaign:{campaignId}");
    }
}
