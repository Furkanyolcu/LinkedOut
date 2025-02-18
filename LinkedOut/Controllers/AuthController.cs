using Microsoft.AspNetCore.Mvc;

namespace LinkedOut.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
