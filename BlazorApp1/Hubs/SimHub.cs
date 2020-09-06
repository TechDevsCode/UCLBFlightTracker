using BlazorApp1.Data;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorApp1.Hubs
{
    public class SimHub : Hub
    {
        //public async Task SendUpdate(Position position)
        //{
        //    await Clients.All.SendAsync("Position", position);
        //}

        //public async Task SendMessage(string message)
        //{
        //    await Clients.All.SendAsync("Message", message);
        //}

        public async Task SendPosition(string lat, string lng)
        {
            await Clients.All.SendAsync("Position", lat, lng);
        }

        public async Task SendPositionObject(Position p)
        {
            var json = JsonConvert.SerializeObject(p, Formatting.None);
            await Clients.All.SendAsync("PositionObject", json);
        }
    }
}
