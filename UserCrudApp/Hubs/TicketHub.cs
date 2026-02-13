using Microsoft.AspNetCore.SignalR;

namespace UserCrudApp.Hubs
{
    public class TicketHub : Microsoft.AspNetCore.SignalR.Hub
    {
        public async Task NotifyTicketUpdated(int ticketId)
        {
            await Clients.All.SendAsync("TicketUpdated", ticketId);
        }
    }
}
