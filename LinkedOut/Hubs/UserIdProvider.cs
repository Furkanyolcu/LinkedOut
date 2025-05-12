using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LinkedOut.Hubs
{
    public class UserIdProvider : Microsoft.AspNetCore.SignalR.IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }
    }
} 