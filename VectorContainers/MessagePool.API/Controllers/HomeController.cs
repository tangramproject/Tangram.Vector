using Microsoft.AspNetCore.Mvc;

namespace MessagePool.API.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return new RedirectResult("~/swagger");
        }
    }
}
