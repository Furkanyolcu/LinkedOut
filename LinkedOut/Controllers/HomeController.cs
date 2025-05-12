using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LinkedOut.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LinkedOut.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Auth");
            }
            return View();
        }

        public IActionResult HomePage()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Json(new List<object>());
            }

            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdStr) || !int.TryParse(currentUserIdStr, out int currentUserId))
            {
                return Unauthorized();
            }

            // Sadece ad ve soyad ile arama yapıyoruz
            var users = await _context.Users
                .Where(u => u.Id != currentUserId)  // Kendimizi sonuçlarda gösterme
                .ToListAsync();

            // In-memory filtreleme
            var searchResults = users
                .Where(u => u.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || 
                           u.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .Select(u => new
                {
                    u.Id,
                    FullName = u.FirstName + " " + u.LastName,
                    Title = u.Headline,
                    ProfileImageUrl = !string.IsNullOrEmpty(u.ProfilePicture) ? 
                        u.ProfilePicture : "/falcon/falcon/public/assets/img/team/avatar.png"
                })
                .Take(10)
                .ToList();

            // Sonuçları günlüğe yazdır (hata ayıklama için)
            System.Diagnostics.Debug.WriteLine($"Search term: {searchTerm}, Results: {searchResults.Count}");
            foreach (var result in searchResults)
            {
                System.Diagnostics.Debug.WriteLine($"User: {result.FullName}");
            }

            return Json(searchResults);
        }
    }
}
