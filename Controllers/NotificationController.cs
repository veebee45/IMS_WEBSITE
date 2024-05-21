using IMSMIS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace IMSMIS.Controllers
{
	public class NotificationController : Controller
	{
		private readonly IConfiguration _configuration;
		public NotificationController(IConfiguration configuration)
		{
			_configuration = configuration;
		}
		public IActionResult Notification()
		{
			List<ProjectInfo> projectInfos = new List<ProjectInfo>();
			string DefaultConnection = _configuration.GetConnectionString("DefaultConnection");

			using (SqlConnection con = new SqlConnection(DefaultConnection))
			{
				con.Open();
				string projectIDQuery = "";
				string role = HttpContext.Session.GetString("UserRole");
				if(role == "Super Admin")
				{
					projectIDQuery = "SELECT ID, Task_ID, Message, Action_User, SA_Flag, UserName from [IMS].[dbo].[Notifications] where Action_User != @User";
				}
				else
				{
					projectIDQuery = "SELECT ID, Task_ID, Message, Action_User, UserFlag, UserName from [IMS].[dbo].[Notifications] where UserName = @User";
				}
				

				using (SqlCommand cmd = new SqlCommand(projectIDQuery, con))
				{
					string user = HttpContext.Session.GetString("UserName");
					cmd.Parameters.AddWithValue("@User", user);

					using (SqlDataReader reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							ProjectInfo projectInfo = new ProjectInfo
							{
								NID = reader.GetInt32(0),
								ProjectID = reader.GetString(1).Trim(),
								Message = reader.GetString(2).Trim(),
								Assigner = reader.GetString(3).Trim(),
								Flag = reader.GetString(4).Trim(),
								Assignee = reader.GetString(5).Trim()
							};
							projectInfos.Add(projectInfo);
						}
					}
				}
			}
			return View(projectInfos);
		}
		[HttpGet]
		public IActionResult GetNotificationCount()
		{
			string DefaultConnection = _configuration.GetConnectionString("DefaultConnection");
			string notificationQuery = "";
			string user = HttpContext.Session.GetString("UserName");
			if (HttpContext.Session.GetString("UserRole") == "Super Admin")
			{
				notificationQuery = "SELECT count(1) FROM [IMS].[dbo].[Notifications] WHERE SA_Flag = 'True' and Action_User != @User";
			}
			else
			{
				notificationQuery = "SELECT count(1) FROM [IMS].[dbo].[Notifications] WHERE UserName = @User and UserFlag = 'True' and Action_User ! = @User";
			}



			int notificationCount = 0;

			try
			{
				using (SqlConnection con = new SqlConnection(DefaultConnection))
				{
					con.Open();
					using (SqlCommand cmd = new SqlCommand(notificationQuery, con))
					{
						cmd.Parameters.AddWithValue("@User", user);
						notificationCount = (int)cmd.ExecuteScalar();
					}
				}
			}
			catch (Exception ex)
			{
				return BadRequest("An error occurred while fetching notification count.");
			}

			return Content(notificationCount.ToString());
		}

		[HttpGet]
		public IActionResult GetProjectIDs()
		{
			List<ProjectInfo> projectInfos = new List<ProjectInfo>();
			string DefaultConnection = _configuration.GetConnectionString("DefaultConnection");

			// Create a SqlConnection object
			using (SqlConnection con = new SqlConnection(DefaultConnection))
			{
				con.Open(); // Open the connection
				string projectIDQuery = "";
				if (HttpContext.Session.GetString("UserRole") == "Super Admin")
				{
					projectIDQuery = "SELECT ID, Task_ID, Message, Action_User, SA_Flag, UserName from [IMS].[dbo].[Notifications] where Action_User != @User";
				}
				else
				{
					projectIDQuery = "SELECT ID, Task_ID, Message, Action_User, UserFlag, UserName from [IMS].[dbo].[Notifications] where UserName = @User and Action_User != @User";
				}

				using (SqlCommand cmd = new SqlCommand(projectIDQuery, con))
				{
					string user = HttpContext.Session.GetString("UserName");
					cmd.Parameters.AddWithValue("@User", user);

					using (SqlDataReader reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							ProjectInfo projectInfo = new ProjectInfo
							{
								NID = reader.GetInt32(0),
								ProjectID = reader.GetString(1).Trim(),
								Message = reader.GetString(2).Trim(),
								Assigner = reader.GetString(3).Trim(),
								Flag = reader.GetString(4).Trim(),
								Assignee = reader.GetString(5).Trim()
							};
							projectInfos.Add(projectInfo);
						}
					}
				}
			}

			return Json(projectInfos);
		}


		[HttpPost]
		public IActionResult ChangeFlag(int ID)
		{
			string DefaultConnection = _configuration.GetConnectionString("DefaultConnection");

			string role = HttpContext.Session.GetString("UserRole");
			string updateFlagQuery = "";
			if (role == "Super Admin")
			{
			    updateFlagQuery = "UPDATE [IMS].[dbo].[Notifications] SET SA_Flag = 'False' WHERE ID = @ID";
			}
			else
			{
				updateFlagQuery = "UPDATE [IMS].[dbo].[Notifications] SET UserFlag = 'False' WHERE ID = @ID AND UserName = @User";
			}
			
			try
			{
				using (SqlConnection con = new SqlConnection(DefaultConnection))
				{
					con.Open();
					using (SqlCommand cmd = new SqlCommand(updateFlagQuery, con))
					{
						string user = HttpContext.Session.GetString("UserName");
						cmd.Parameters.AddWithValue("@ID", ID);
						cmd.Parameters.AddWithValue("@User", user);
						cmd.ExecuteNonQuery();
					}
				}
				return Ok(); // Successful flag change
			}
			catch (Exception ex)
			{
				return BadRequest("An error occurred while changing the flag.");
			}
		}

		
	}
}
