using Microsoft.AspNetCore.Mvc;

namespace LinkedOut.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult HomePage()
        {
            return View();
        }
    }
}
