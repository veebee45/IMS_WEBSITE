using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace IMSMIS.Controllers
{
	public class NHAController : Controller
	{
		public IActionResult NHA()
		{
			return View();
		}

		[HttpPost]
		public ActionResult HandleButtonClick(string value)
		{
			if (HttpContext.Session.GetString("UserRole") == null)
			{
				return View("Index");
				//return RedirectToAction("Index", "Auth");
			}
			try
			{
				string exePath = "";
				switch (value)
				{
					case "1":
						exePath = "E:\\IMS-TOOL\\NHA_TOOL\\NHA_TOOL.exe";
						break;
					case "2":
						exePath = "E:\\IMS-TOOL\\NHA_REMAKE\\NHA_REMAKE.exe";
						break;
				}
				Process.Start(exePath);
				return Json(new { success = true });
			}
			catch (System.ComponentModel.Win32Exception ex)
			{
				return Json(new { success = false, error = "File not found" });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, error = ex.Message });
			}
		}
	}
}
