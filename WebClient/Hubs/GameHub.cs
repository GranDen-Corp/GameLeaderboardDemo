using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace WebClient.Hubs
{
    public class GameHub : Hub
    {
        public async Task BroadcastLeaderboardUpdate()
        {
            await Clients.All.SendAsync("BroadcastLeaderboardUpdate");
        }
    }
}
