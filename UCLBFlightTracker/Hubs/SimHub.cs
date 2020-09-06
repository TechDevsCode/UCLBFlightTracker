using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace UCLBFlightTracker.Hubs
{
    public class SimHub : Hub
    {
        public async Task SendPositionObject(Position p)
        {
            var json = JsonConvert.SerializeObject(p, Formatting.None);
            await Clients.All.SendAsync("PositionObject", json);
        }

    }
}
