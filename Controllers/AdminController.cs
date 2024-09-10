using Microsoft.AspNetCore.Mvc;

namespace RestaurantBookingFrontend.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }
    }
}
