using Microsoft.AspNetCore.Mvc;
using LinkedOut.Models;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace LinkedOut.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string Username, string Password)
        {
            var user = _context.Users.FirstOrDefault(u => u.FirstName == Username && u.PasswordHash == Password);
            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FirstName),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email)
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal).Wait();
                return RedirectToAction("HomePage", "Home");
            }
            TempData["Error"] = "Kullanıcı adı veya şifre hatalı!";
            return View("~/Views/Auth/Index.cshtml");
        }

        [HttpPost]
        public IActionResult Register(string Username, string Email, string Password)
        {
            if (_context.Users.Any(u => u.Email == Email || u.FirstName == Username))
            {
                TempData["Error2"] = "Bu kullanıcı adı veya e-posta zaten kullanılıyor.";
                return View("~/Views/Auth/Index.cshtml");
            }

            var newUser = new User
            {
                FirstName = Username,
                LastName = string.Empty,
                Email = Email,
                PasswordHash = Password,
                ProfilePicture = string.Empty,
                Headline = string.Empty,
                About = string.Empty,
                Location = string.Empty,
                Website = string.Empty
            };
            _context.Users.Add(newUser);
            _context.SaveChanges();

            TempData["Success2"] = "Kayıt işlemi başarılı! Şimdi giriş yapabilirsiniz.";
            return View("~/Views/Auth/Index.cshtml");
        }
    }
} 