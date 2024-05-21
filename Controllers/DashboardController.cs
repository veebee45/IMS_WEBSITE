using Microsoft.AspNetCore.Mvc;

namespace IMSMIS.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
