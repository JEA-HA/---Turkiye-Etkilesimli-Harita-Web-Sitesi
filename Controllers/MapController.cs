using Microsoft.AspNetCore.Mvc;

namespace TurkeyCityGuide.Controllers
{
    public class MapController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
