using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinkedOut.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using LinkedOut.Hubs;

namespace LinkedOut.Controllers
{
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public MessageController(ApplicationDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetConversations()
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdStr))
                return Unauthorized();
            int currentUserId = int.Parse(currentUserIdStr);

            var conversations = await _context.Messages
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .GroupBy(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                .Select(g => new
                {
                    UserId = g.Key,
                    UserName = _context.Users
                        .Where(u => u.Id == g.Key)
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefault(),
                    LastMessage = g.OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.Content)
                        .FirstOrDefault(),
                    LastMessageTime = g.Max(m => m.CreatedAt),
                    UnreadCount = g.Count(m => m.ReceiverId == currentUserId && !m.IsRead)
                })
                .OrderByDescending(c => c.LastMessageTime)
                .ToListAsync();

            return Json(conversations);
        }

        [HttpGet]
        public async Task<IActionResult> Chat(int id)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdStr))
                return Unauthorized();
            int currentUserId = int.Parse(currentUserIdStr);

            var otherUser = await _context.Users.FindAsync(id);
            if (otherUser == null)
                return NotFound();

            var messages = await _context.Messages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == id) ||
                           (m.SenderId == id && m.ReceiverId == currentUserId))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            // Mark messages as read
            var unreadMessages = messages.Where(m => m.ReceiverId == currentUserId && !m.IsRead);
            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }
            await _context.SaveChangesAsync();

            // Notify sender through SignalR that messages were read
            foreach (var message in unreadMessages)
            {
                await _hubContext.Clients.User(message.SenderId.ToString())
                    .SendAsync("MessageRead", message.Id);
            }

            ViewBag.CurrentUserId = currentUserId;
            ViewBag.OtherUserId = id;
            ViewBag.OtherUserName = otherUser.FirstName + " " + otherUser.LastName;

            return PartialView(messages);
        }

        [HttpPost]
        public async Task<IActionResult> Send(int receiverId, string content)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdStr))
                return Unauthorized();
            int currentUserId = int.Parse(currentUserIdStr);

            var receiver = await _context.Users.FindAsync(receiverId);
            if (receiver == null)
                return NotFound();

            var message = new Message
            {
                SenderId = currentUserId,
                ReceiverId = receiverId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Send message through SignalR
            var currentUser = await _context.Users.FindAsync(currentUserId);
            var senderName = currentUser.FirstName + " " + currentUser.LastName;

            await _hubContext.Clients.User(receiverId.ToString())
                .SendAsync("ReceiveMessage", new
                {
                    id = message.Id,
                    senderId = currentUserId,
                    senderName = senderName,
                    content = content,
                    timestamp = message.CreatedAt
                });

            return Json(new { success = true, messageId = message.Id });
        }
    }
} 