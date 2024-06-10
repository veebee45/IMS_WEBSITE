using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace IMSMIS.Controllers
{
	public class ToolsController : Controller
	{
        private readonly IConfiguration _configuration;
        public ToolsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Tools()
		{
            DataTable dataTable = new DataTable();

            try
            {
                using (SqlConnection connect = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connect.Open();

                    using (SqlCommand commandd = new SqlCommand($"select * from [dbo].[ProjectMaster] where ProjectExe='Yes'", connect))
                    {
                        using (SqlDataReader reader = commandd.ExecuteReader())
                        {
                            dataTable.Load(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return View(dataTable);
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

		public IActionResult ToolsView(string ProjectName)
        {
            ViewBag.ProjectName = ProjectName;
            return View();
        }

		public IActionResult AddTool(string ProjectName)
		{
            ViewBag.ProjectName = ProjectName;
            return View();
		}
	}
}
