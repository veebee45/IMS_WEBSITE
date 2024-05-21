using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace IMSMIS.Controllers
{
	public class EpicController : Controller
	{
		public IActionResult Epic()
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
						exePath = "E:\\IMS-TOOL\\EPIC_TOOL\\EPIC_AUTOMATED.exe";
						break;
					case "2":
						exePath = "E:\\IMS-TOOL\\EPIC_REMAKE\\EPIC_REMAKE.exe";
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
