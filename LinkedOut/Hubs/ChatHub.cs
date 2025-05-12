using Microsoft.AspNetCore.SignalR;
using LinkedOut.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace LinkedOut.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(int receiverId, string message)
        {
            var httpContext = Context.GetHttpContext();
            var senderIdStr = httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrEmpty(senderIdStr) || !int.TryParse(senderIdStr, out int senderId))
            {
                throw new HubException("Unauthorized");
            }

            var receiver = await _context.Users.FindAsync(receiverId);
            if (receiver == null)
            {
                throw new HubException("Receiver not found");
            }

            // Save the message to database
            var newMessage = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = message,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(newMessage);
            await _context.SaveChangesAsync();

            // Send the message to the specific user
            await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", new
            {
                id = newMessage.Id,
                senderId = senderId,
                senderName = (await _context.Users.FindAsync(senderId))?.FirstName + " " + 
                             (await _context.Users.FindAsync(senderId))?.LastName,
                content = message,
                timestamp = newMessage.CreatedAt
            });

            // Send confirmation back to the sender
            await Clients.Caller.SendAsync("MessageSent", new
            {
                id = newMessage.Id,
                receiverId = receiverId,
                content = message,
                timestamp = newMessage.CreatedAt
            });
        }

        public async Task MarkAsRead(int messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message != null)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();

                // Notify the sender that their message has been read
                await Clients.User(message.SenderId.ToString()).SendAsync("MessageRead", messageId);
            }
        }

        public async Task JoinChat(int otherUserId)
        {
            var httpContext = Context.GetHttpContext();
            var userIdStr = httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                throw new HubException("Unauthorized");
            }

            var otherUser = await _context.Users.FindAsync(otherUserId);
            if (otherUser == null)
            {
                throw new HubException("User not found");
            }

            // Mark unread messages as read
            var unreadMessages = await _context.Messages
                .Where(m => m.SenderId == otherUserId && m.ReceiverId == userId && !m.IsRead)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();

            // Notify the sender that their messages have been read
            foreach (var message in unreadMessages)
            {
                await Clients.User(message.SenderId.ToString()).SendAsync("MessageRead", message.Id);
            }
        }
    }
} 