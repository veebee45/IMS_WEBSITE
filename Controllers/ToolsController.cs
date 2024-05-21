using Microsoft.AspNetCore.Mvc;

namespace IMSMIS.Controllers
{
	public class ToolsController : Controller
	{
		public IActionResult Tools()
		{
			return View();
		}

		public IActionResult EPIC()
		{
			return RedirectToAction("Epic", "Epic");
		}
		public IActionResult Disability()
		{
			return RedirectToAction("Disability", "Disability");
		}
		public IActionResult NHA()
		{
			return RedirectToAction("NHA", "NHA");
		}
	}
}
