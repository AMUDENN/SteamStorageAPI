using Microsoft.AspNetCore.SignalR;

namespace LoginWebApp.Utilities.TokenHub;

public class TokenHub : Hub
{
    public async Task SendToken(string group, string token)
    {
        await Clients.Group(group).SendAsync("Token", token);
    }
    public async Task JoinGroup(string group)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
    }
}