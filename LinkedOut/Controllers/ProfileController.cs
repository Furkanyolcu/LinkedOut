using Microsoft.AspNetCore.Mvc;

namespace LinkedOut.Controllers
{
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
