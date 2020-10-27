using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace tdaStreamHub.Hubs
{
    public class TDAHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            if (Clients != null)
                await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
        public async Task SendTopic(string topic, string contents)
        {
            if (Clients != null)
                await Clients.All.SendAsync(topic, topic, contents);
        }
    }
}
