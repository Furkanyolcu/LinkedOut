using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LinkedOut.Models;
using System.Security.Claims;

namespace LinkedOut.Controllers
{
    [Authorize]
    public class ConnectionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConnectionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var connections = await _context.Connections
                .Where(c => (c.RequesterId == userId || c.AddresseeId == userId) &&
                           c.Status == ConnectionStatus.Accepted)
                .Include(c => c.Requester)
                .Include(c => c.Addressee)
                .ToListAsync();

            var pendingRequests = await _context.Connections
                .Where(c => c.AddresseeId == userId && c.Status == ConnectionStatus.Pending)
                .Include(c => c.Requester)
                .ToListAsync();

            ViewBag.PendingRequests = pendingRequests;
            return View(connections);
        }

        [HttpPost]
        public async Task<IActionResult> SendRequest(int addresseeId)
        {
            var requesterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Check if connection already exists
            var existingConnection = await _context.Connections
                .FirstOrDefaultAsync(c => (c.RequesterId == requesterId && c.AddresseeId == addresseeId) ||
                                        (c.RequesterId == addresseeId && c.AddresseeId == requesterId));

            if (existingConnection != null)
            {
                return BadRequest("Connection request already exists");
            }

            var connection = new Connection
            {
                RequesterId = requesterId,
                AddresseeId = addresseeId,
                Status = ConnectionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Connections.Add(connection);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var connection = await _context.Connections
                .FirstOrDefaultAsync(c => c.Id == id && c.AddresseeId == userId);

            if (connection == null)
            {
                return NotFound();
            }

            connection.Status = ConnectionStatus.Accepted;
            connection.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RejectRequest(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var connection = await _context.Connections
                .FirstOrDefaultAsync(c => c.Id == id && c.AddresseeId == userId);

            if (connection == null)
            {
                return NotFound();
            }

            connection.Status = ConnectionStatus.Rejected;
            connection.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveConnection(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var connection = await _context.Connections
                .FirstOrDefaultAsync(c => c.Id == id &&
                                        (c.RequesterId == userId || c.AddresseeId == userId));

            if (connection == null)
            {
                return NotFound();
            }

            _context.Connections.Remove(connection);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Block(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var connection = await _context.Connections
                .FirstOrDefaultAsync(c => c.Id == id &&
                                        (c.RequesterId == userId || c.AddresseeId == userId));

            if (connection == null)
            {
                return NotFound();
            }

            connection.Status = ConnectionStatus.Blocked;
            connection.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
} 