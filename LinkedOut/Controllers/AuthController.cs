using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using LinkedOut.Models;
using Microsoft.AspNetCore.Authorization;

public class AuthController : Controller
{
    private readonly Context _context;

    public AuthController(Context context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
    [HttpPost]
    [AllowAnonymous]
    public IActionResult Login(string Username, string password)
    {
        if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(password))
        {
            TempData["Error"] = "Kullanıcı adı ve Şifre alanları doldurulmalıdır.";
            return RedirectToAction("Index");
        }

        var user = _context.Auths.FirstOrDefault(u =>
            u.Username == Username &&
            u.Password == password &&
            u.isActive);

        if (user != null)
        {
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Email", user.Email);
            TempData["Success"] = "Giriş başarılı!";
        }
        else
        {
            TempData["Error"] = "Giriş bilgileri hatalı veya kullanıcı aktif değil.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [AllowAnonymous]
    public IActionResult Register(string username, string email, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            TempData["Error2"] = "Tüm alanları doldurmalısınız.";
            return RedirectToAction("Index");
        }

        if (_context.Auths.Any(u => u.Email == email || u.Username == username))
        {
            TempData["Error2"] = "Bu kullanıcı adı veya e-posta zaten kullanılıyor.";
            return RedirectToAction("Index");
        }

        var newUser = new Auth
        {
            Username = username,
            Email = email,
            Password = password, // Şifre düz metin olarak kaydediliyor.
            isActive = true
        };

        _context.Auths.Add(newUser);
        _context.SaveChanges();

        TempData["Success2"] = "Kayıt işlemi başarılı! Şimdi giriş yapabilirsiniz.";
        return RedirectToAction("Index");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index");
    }
}
