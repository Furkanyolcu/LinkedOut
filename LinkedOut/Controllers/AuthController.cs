using Microsoft.AspNetCore.Mvc;
using LinkedOut.Models;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

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
            try
            {
                // Only select the specific columns we need to avoid the CoverPhoto column issue
                var user = _context.Users
                    .Where(u => u.FirstName == Username && u.PasswordHash == Password)
                    .Select(u => new
                    {
                        u.Id,
                        u.FirstName,
                        u.Email
                    })
                    .FirstOrDefault();
                    
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
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                TempData["Error"] = "Giriş işlemi sırasında bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
                return View("~/Views/Auth/Index.cshtml");
            }
        }

        [HttpPost]
        public IActionResult Register(string Username, string Email, string Password)
        {
            try
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
                    CoverPhoto = string.Empty,
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
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Register error: {ex.Message}");
                TempData["Error2"] = "Kayıt işlemi sırasında bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
                return View("~/Views/Auth/Index.cshtml");
            }
        }
    }
} 