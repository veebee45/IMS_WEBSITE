using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace IMSMIS.Controllers
{
	public class DisabilityController : Controller
	{
		public IActionResult Disability()
		{
			return View();
		}

		[HttpPost]
		public ActionResult HandleButtonClick(string value)
		{
			try
			{
				string exePath = "";
				switch (value)
				{
					case "1":
						exePath = "E:\\DisabilityTool\\Disability_API.exe";
						break;
					case "2":
						//exePath = "C:\\IMS-TOOL\\EPIC_REMAKE\\EPIC_REMAKE.exe";
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
