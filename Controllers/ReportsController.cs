using Microsoft.AspNetCore.Mvc;
using IMSMIS.Models;
using Microsoft.Data.SqlClient;

namespace IMSMIS.Controllers
{
	public class ReportsController : Controller
	{
		private readonly IConfiguration _configuration;

		public ReportsController(IConfiguration configuration, IWebHostEnvironment webHost)
		{
			_configuration = configuration;
		}
		public IActionResult Reports()
		{
			List<IMSMIS.Models.Data> dataList = new List<IMSMIS.Models.Data>();
			string query = "SELECT Project, SUM(TotalData) AS TotalCount FROM [IMS].[dbo].[Data] GROUP BY Project";

			// Create connection and command objects
			using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
			{
				connection.Open();

				using (SqlCommand command = new SqlCommand(query, connection))
				{
					SqlDataReader reader = command.ExecuteReader();

					// Read the data and populate the list
					while (reader.Read())
					{
						IMSMIS.Models.Data data = new IMSMIS.Models.Data();
						data.Project = reader["Project"].ToString();
						data.totaldata = (int)reader["TotalCount"];
						dataList.Add(data);
					}
				}
			}

			return View(dataList);

		}
	}
}
