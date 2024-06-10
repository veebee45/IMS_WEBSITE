using System.Data;
using IMSMIS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using IMS_MIS.Classes;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace IMSMIS.Controllers
{
	public class TasksController : Controller
	{
		private readonly IConfiguration _configuration;
		private readonly IWebHostEnvironment _webHost;

		public TasksController(IConfiguration configuration, IWebHostEnvironment webHost)
		{
			_configuration = configuration;
			_webHost = webHost;
		}

		/*-----------------------------------------Display Tasks list on Page-----------------------------------------------------------*/

		public IActionResult Tasks(int page = 1, int pageSize = 15, List<string> assignees = null, List<string> statuses = null, List<string> priorities = null, List<string> projects = null)
		{
			DataTable dataTable = new DataTable();
			int totalRows = 0;
			int pageCount = 0;
			List<string> uniqueAssignees = new List<string>();
			List<string> uniqueProjects = new List<string>();
			string filterCondition = "WHERE Flag = 'True'";

			if (assignees != null && assignees.Any())
			{
				string assigneeFilter = string.Join(",", assignees.Select(a => $"'{a}'"));
				filterCondition += $" AND Assignee IN ({assigneeFilter})";
			}

			if (statuses != null && statuses.Any())
			{
				string statusFilter = string.Join(",", statuses.Select(s => $"'{s}'"));
				filterCondition += $" AND Status IN ({statusFilter})";
			}

			if (priorities != null && priorities.Any())
			{
				string priorityFilter = string.Join(",", priorities.Select(p => $"'{p}'"));
				filterCondition += $" AND Priority IN ({priorityFilter})";
			}

			if (projects != null && projects.Any())
			{
				string projectFilter = string.Join(",", projects.Select(p => $"'{p}'"));
				filterCondition += $" AND Project IN ({projectFilter})";
			}

			try
			{
				using (SqlConnection connect = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
				{
					connect.Open();

					using (SqlCommand countCommand = new SqlCommand($"SELECT COUNT(*) FROM [IMS].[dbo].[Tasks] {filterCondition}", connect))
					{
						totalRows = (int)countCommand.ExecuteScalar();
					}

					pageCount = (int)Math.Ceiling((double)totalRows / pageSize);
					int skip = (page - 1) * pageSize;

					using (SqlCommand command = new SqlCommand($"SELECT * FROM [IMS].[dbo].[Tasks] {filterCondition} ORDER BY Created DESC OFFSET {skip} ROWS FETCH NEXT {pageSize} ROWS ONLY", connect))
					{
						using (SqlDataReader reader = command.ExecuteReader())
						{
							dataTable.Load(reader);
						}
					}

					using (SqlCommand uniqueAssigneesCommand = new SqlCommand("SELECT DISTINCT UserName FROM [IMS].[dbo].[UserDetails]", connect))
					{
						using (SqlDataReader uniqueAssigneesReader = uniqueAssigneesCommand.ExecuteReader())
						{
							while (uniqueAssigneesReader.Read())
							{
								uniqueAssignees.Add(uniqueAssigneesReader.GetString(0));
							}
						}
					}

					using (SqlCommand uniqueProjectsCommand = new SqlCommand("SELECT DISTINCT Projects FROM [IMS].[dbo].[ID_MASTER]", connect))
					{
						using (SqlDataReader uniqueProjectsReader = uniqueProjectsCommand.ExecuteReader())
						{
							while (uniqueProjectsReader.Read())
							{
								uniqueProjects.Add(uniqueProjectsReader.GetString(0));
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				// Handle exception
			}

			ViewBag.PageCount = pageCount;
			ViewBag.CurrentPage = page;
			ViewBag.UniqueAssignees = uniqueAssignees;
			ViewBag.UniqueProjects = uniqueProjects;
			ViewBag.SelectedAssignees = assignees;
			ViewBag.SelectedStatuses = statuses;
			ViewBag.SelectedPriorities = priorities;
			ViewBag.SelectedProjects = projects;

			return View(dataTable);
		}



		/*-----------------------------------------Add New Tasks-----------------------------------------------------------*/


		public IActionResult AddTask()
		{
			List<string> assignees = new List<string>();
			List<string> projects = new List<string>();
			try
			{
				using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
				{
					con.Open();
					using (SqlCommand cmd = new SqlCommand("SELECT Distinct UserName FROM [IMS].[dbo].[UserDetails]", con))
					{
						using (SqlDataReader reader = cmd.ExecuteReader())
						{
							while (reader.Read())
							{
								string assignee = reader.GetString(0);
								assignees.Add(assignee);
							}
						}
					}

					using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT Projects FROM [IMS].[dbo].[ID_MASTER]", con))
					{
						using (SqlDataReader reader = cmd.ExecuteReader())
						{
							while (reader.Read())
							{
								string project = reader.GetString(0);
								projects.Add(project);
							}
						}
					}
				}


			}
			catch (Exception ex)
			{
			}
			string User = HttpContext.Session.GetString("UserName");
			User = User.Replace(" ", "");
			string tempFolderPath = Path.Combine(_webHost.WebRootPath, "Upload", $"temp_{User}");
			if (Directory.Exists(tempFolderPath))
			{
				var files = Directory.GetFiles(tempFolderPath);
				foreach (var file in files)
				{
					System.IO.File.Delete(file);
				}
			}
			ViewBag.Assignees = assignees;
			ViewBag.Projects = projects;

			return View();
		}

		/*-----------------------------------------Create the Task-----------------------------------------------------------*/


		[HttpPost]
		//public IActionResult SaveTask(Tasks tasks)
		//{
		//	string ProjectID = "";
		//	try
		//	{
		//		string user = HttpContext.Session.GetString("UserName");

		//		using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
		//		{

		//			con.Open();
		//			using (SqlCommand cmd = new SqlCommand("usp_SaveTask", con))
		//			{
		//				cmd.CommandType = System.Data.CommandType.StoredProcedure;
		//				cmd.Parameters.AddWithValue("@Lot_ID", GetNextIDForProject(tasks.Project));
		//				cmd.Parameters.AddWithValue("@Summary", tasks.Summary);
		//				cmd.Parameters.AddWithValue("@Description", tasks.Description);
		//				cmd.Parameters.AddWithValue("@Project", tasks.Project);
		//				cmd.Parameters.AddWithValue("@Priority", tasks.Priority);
		//				cmd.Parameters.AddWithValue("@Status", tasks.Status);
		//				cmd.Parameters.AddWithValue("@Assignee", tasks.Assignee);
		//				cmd.Parameters.AddWithValue("@Created", tasks.StartDate);
		//				cmd.Parameters.AddWithValue("@EndDate", tasks.EndDate);
		//				cmd.Parameters.AddWithValue("@Type", tasks.Type);
		//				cmd.Parameters.AddWithValue("@EstimatedDays", tasks.EstimatedDays);
		//				cmd.Parameters.AddWithValue("@CreatedBy", user);

		//				ProjectID = tasks.Project + '-' + GetNextIDForProject(tasks.Project);
		//				string filename = tasks.filenames;
		//				if (!string.IsNullOrEmpty(filename))
		//				{
		//					string[] fileNamesArray = filename.Split(',').Select(f => f.Trim()).ToArray();

		//					foreach (string fileName in fileNamesArray)
		//					{
		//						string User = user.Replace(" ", "");
		//						string projectFolder = Path.Combine(_webHost.WebRootPath, "Upload", tasks.Project);
		//						string projectSubFolder = Path.Combine(projectFolder, ProjectID.ToString());
		//						string tempFilePath = Path.Combine(_webHost.WebRootPath, "Upload", $"temp_{User}", fileName.Trim());
		//						string targetFilePath = Path.Combine(projectSubFolder, fileName.Trim());

		//						if (!Directory.Exists(projectFolder))
		//						{
		//							Directory.CreateDirectory(projectFolder);
		//						}

		//						if (!Directory.Exists(projectSubFolder))
		//						{
		//							Directory.CreateDirectory(projectSubFolder);
		//						}

		//						System.IO.File.Move(tempFilePath, targetFilePath);

		//						using (SqlCommand filecmd = new SqlCommand("INSERT INTO TaskFiles (Project, ProjectId, FileName, FilePath) VALUES (@Project, @ProjectID, @FileName, @FilePath)", con))
		//						{
		//							string filepath = Path.Combine(projectSubFolder, fileName);
		//							filecmd.Parameters.AddWithValue("@Project", tasks.Project);
		//							filecmd.Parameters.AddWithValue("@ProjectID", ProjectID);
		//							filecmd.Parameters.AddWithValue("@FileName", fileName);
		//							filecmd.Parameters.AddWithValue("@FilePath", filepath);
		//							filecmd.ExecuteNonQuery();
		//						}
		//					}
		//				}


		//				cmd.ExecuteNonQuery();

		//				string assigneeEmail;
		//				using (SqlCommand getEmailCmd = new SqlCommand("SELECT [Email Id] FROM [IMS].[dbo].[UserDetails] WHERE UserName = @UserID", con))
		//				{
		//					getEmailCmd.Parameters.AddWithValue("@UserID", tasks.Assignee);
		//					assigneeEmail = (string)getEmailCmd.ExecuteScalar();
		//				}

		//				using (SqlCommand notification = new SqlCommand("INSERT INTO Notifications (Task_ID, UserName, Action_User, Notification_type, Message, Created_at, UserFlag, SA_Flag) VALUES (@ProjectID, @UserName, @ActionUser, @Type, @Message, @Time, @Flag, @Flag)", con))
		//				{
		//					notification.Parameters.AddWithValue("@ProjectID", ProjectID);
		//					notification.Parameters.AddWithValue("@UserName", tasks.Assignee);
		//					notification.Parameters.AddWithValue("@ActionUser", user);
		//					notification.Parameters.AddWithValue("@Type", "Task Creation");
		//					notification.Parameters.AddWithValue("@Message", $"created a task {ProjectID} and assigned to");
		//					notification.Parameters.AddWithValue("@Time", DateTime.Now);
		//					notification.Parameters.AddWithValue("@Flag", "True");
		//					notification.ExecuteNonQuery();

		//				}

		//				string assigner = HttpContext.Session.GetString("UserName");
		//				string host = _configuration.GetSection("EmailSettings:SmtpHost").Value;
		//				int port = Convert.ToInt32(_configuration.GetSection("EmailSettings:SmtpPort").Value);
		//				string sender = _configuration.GetSection("EmailSettings:EmailSender").Value;
		//				string sender_password = _configuration.GetSection("EmailSettings:EmailPassword").Value;
		//				string currentDateTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm");
		//				string subject = "Task Created";
		//				string body = "<!DOCTYPE html>" +
		//						  "<html lang=\"en\">" +
		//						  "<head>" +
		//						  "<meta charset=\"UTF-8\">" +
		//						  "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
		//						  "<title>New Task</title>" +
		//						  "</head>" +
		//						  "<body>" +
		//						  "<div style=\"font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;\">" +
		//						  $"<p style=\"color: #333;\">Dear {tasks.Assignee},</p>" +
		//						  $"<p style=\"color: #333;\">Task was created by <strong>{assigner}</strong> in {tasks.Project} at {currentDateTime}.</p>" +
		//						  "<ul style=\"color: #333; list-style-type: none; padding-left: 0;\">" +
		//						  $"<li><strong>Task:</strong> {ProjectID}</li>" +
		//						  $"<li><strong>Priority:</strong> {tasks.Priority}</li>" +
		//						  $"<li><strong>Summary:</strong> {tasks.Summary}</li>" +
		//						  $"<li><strong>Description:</strong> {tasks.Description}</li>" +

		//						  "</ul>" +
		//						  "<p style=\"color: #333;\">Best regards,<br>IMS-MIS Team</p>" +
		//						  "</div>" +
		//						  "</body>" +
		//						  "</html>";
		//				EmailSender.SendEmail(sender, sender_password, host, port, true, assigneeEmail, subject, body);

		//			}
		//		}
		//		TempData["SuccessMessage"] = "Task created successfully.";
		//		return RedirectToAction("Details", "Tasks", new { id = ProjectID });
		//	}
		//	catch (Exception ex)
		//	{
		//		TempData["ErrorMessage"] = "Some Error Occured";
		//	}
		//	return RedirectToAction("Tasks", "Tasks");
		//}

		//__________________________new_code___________________________________________
		public async Task<IActionResult> SaveTask(Tasks tasks)
		{
			int next_id = GetNextIDForProject(tasks.Project);
			
			string ProjectID = "";
			string fileType = "TaskFile";
			try
			{
				string user = HttpContext.Session.GetString("UserName");

				using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
				{
					await con.OpenAsync();

					using (SqlCommand cmd = new SqlCommand("usp_SaveTask", con))
					{
						cmd.CommandType = CommandType.StoredProcedure;
						cmd.Parameters.AddWithValue("@Lot_ID", next_id);
						cmd.Parameters.AddWithValue("@Summary", tasks.Summary);
						cmd.Parameters.AddWithValue("@Description", tasks.Description);
						cmd.Parameters.AddWithValue("@Project", tasks.Project);
						cmd.Parameters.AddWithValue("@Priority", tasks.Priority);
						cmd.Parameters.AddWithValue("@Status", tasks.Status);
						cmd.Parameters.AddWithValue("@Assignee", tasks.Assignee);
						cmd.Parameters.AddWithValue("@Created", DateTime.Now);
						cmd.Parameters.AddWithValue("@EndDate", tasks.EndDate);
						cmd.Parameters.AddWithValue("@Type", tasks.Type);
						cmd.Parameters.AddWithValue("@EstimatedDays", tasks.EstimatedDays);
						cmd.Parameters.AddWithValue("@CreatedBy", user);

						await cmd.ExecuteNonQueryAsync();


						ProjectID = tasks.Project + '-' + next_id;


						using (SqlCommand notification = new SqlCommand("INSERT INTO Notifications (Task_ID, UserName, Action_User, Notification_type, Message, Created_at, UserFlag, SA_Flag) VALUES (@ProjectID, @UserName, @ActionUser, @Type, @Message, @Time, @Flag, @Flag)", con))
						{
							notification.Parameters.AddWithValue("@ProjectID", ProjectID);
							notification.Parameters.AddWithValue("@UserName", tasks.Assignee);
							notification.Parameters.AddWithValue("@ActionUser", user);
							notification.Parameters.AddWithValue("@Type", "Task Creation");
							notification.Parameters.AddWithValue("@Message", $"created a task {ProjectID} and assigned to");
							notification.Parameters.AddWithValue("@Time", DateTime.Now);
							notification.Parameters.AddWithValue("@Flag", "True");
							await notification.ExecuteNonQueryAsync();
						}

						string filename = tasks.filenames;
						if (!string.IsNullOrEmpty(filename))
						{
							string[] fileNamesArray = filename.Split(',').Select(f => f.Trim()).ToArray();
							string projectFolder = Path.Combine(_webHost.WebRootPath, "Upload", tasks.Project);
							string projectSubFolder = Path.Combine(projectFolder, ProjectID.ToString());

							if (!Directory.Exists(projectFolder))
							{
								Directory.CreateDirectory(projectFolder);
							}
							if (!Directory.Exists(projectSubFolder))
							{
								Directory.CreateDirectory(projectSubFolder);
							}

							foreach (string fileName in fileNamesArray)
							{
								string User = user.Replace(" ", "");
								string tempFilePath = Path.Combine(_webHost.WebRootPath, "Upload", $"temp_{User}", fileName.Trim());
								string targetFilePath = Path.Combine(projectSubFolder, fileName.Trim());

								System.IO.File.Move(tempFilePath, targetFilePath);

								await InsertFileDetailsAsync(con,  targetFilePath, ProjectID,fileType, tasks);
							}
						}

						// Fire-and-forget email sending

						FireAndForgetEmailSending(tasks, ProjectID, user);
						
						await con.CloseAsync();
						con.Close();
					}

					TempData["SuccessMessage"] = "Task created successfully.";
					return RedirectToAction("Details", "Tasks", new { id = ProjectID });
				}
			}
			catch (Exception ex)
			{
				TempData["ErrorMessage"] = "Some Error Occurred";
				// Log the exception
				// _logger.LogError(ex, "An error occurred while saving the task.");
			}
			return RedirectToAction("Tasks", "Tasks");
		}

		private void FireAndForgetEmailSending(Tasks tasks, string projectID, string user)
		{
			Task.Run(async () =>
			{
				try
				{
					using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
					{
						await con.OpenAsync();
						await SendTaskCreatedEmailAsync(tasks, projectID, con, user);
						con.Close();
					}
					
				}
				catch (Exception ex)
				{
					// Log the exception if necessary
					Console.WriteLine("Failed to send email: " + ex.Message);
				}
			});
		}

		private async Task InsertFileDetailsAsync(SqlConnection con,string filePath, string ProjectId,string fileType, Tasks tasks )
		{
			using (SqlCommand filecmd = new SqlCommand("INSERT INTO TaskFiles (Project, ProjectId, FileName, FilePath, FileType) VALUES (@Project, @ProjectID, @FileName, @FilePath, @FileType)", con))
			{
				filecmd.Parameters.AddWithValue("@Project", tasks.Project);
				filecmd.Parameters.AddWithValue("@ProjectID", ProjectId);
				filecmd.Parameters.AddWithValue("@FileName", tasks.filenames);
				filecmd.Parameters.AddWithValue("@FilePath", filePath);
				filecmd.Parameters.AddWithValue("@FileType", fileType);
				await filecmd.ExecuteNonQueryAsync();
			}
		
		}

		private async Task SendTaskCreatedEmailAsync(Tasks tasks, string projectID, SqlConnection con, string user)
		{
			
				string assigneeEmail;
				using (SqlCommand getEmailCmd = new SqlCommand("SELECT [Email Id] FROM [IMS].[dbo].[UserDetails] WHERE UserName = @UserID", con))
				{
					getEmailCmd.Parameters.AddWithValue("@UserID", tasks.Assignee);
					assigneeEmail = (string)getEmailCmd.ExecuteScalar();
				}

				
				string assigner = HttpContext.Session.GetString("UserName");
				string host = _configuration.GetSection("EmailSettings:SmtpHost").Value;
				int port = Convert.ToInt32(_configuration.GetSection("EmailSettings:SmtpPort").Value);
				string sender = _configuration.GetSection("EmailSettings:EmailSender").Value;
				string sender_password = _configuration.GetSection("EmailSettings:EmailPassword").Value;
				string currentDateTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm");
				string subject = "Task Created";
				string body = "<!DOCTYPE html>" +
						  "<html lang=\"en\">" +
						  "<head>" +
						  "<meta charset=\"UTF-8\">" +
						  "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
						  "<title>New Task</title>" +
						  "</head>" +
						  "<body>" +
						  "<div style=\"font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;\">" +
						  $"<p style=\"color: #333;\">Dear {tasks.Assignee},</p>" +
						  $"<p style=\"color: #333;\">Task was created by <strong>{assigner}</strong> in {tasks.Project} at {currentDateTime}.</p>" +
						  "<ul style=\"color: #333; list-style-type: none; padding-left: 0;\">" +
						  $"<li><strong>Task:</strong> {projectID}</li>" +
						  $"<li><strong>Priority:</strong> {tasks.Priority}</li>" +
						  $"<li><strong>Summary:</strong> {tasks.Summary}</li>" +
						  $"<li><strong>Description:</strong> {tasks.Description}</li>" +

						  "</ul>" +
						  "<p style=\"color: #333;\">Best regards,<br>IMS-MIS Team</p>" +
						  "</div>" +
						  "</body>" +
						  "</html>";
				EmailSender.SendEmail(sender, sender_password, host, port, true, assigneeEmail, subject, body);
			}


		/*----------------------------------------- Insert Data Count -----------------------------------------------------------*/


		[HttpPost]
		public JsonResult InsertFormData(int processedData, int rejectedData, string project, string projectID, string Assignee, string State)
		{

			string connectionString = _configuration.GetConnectionString("DefaultConnection");

			string querycheck = "Select COUNT(1) FROM [IMS].[dbo].[Data] where ProjectID = @ProjectID";
			string queryinsert = "INSERT INTO Data (ProcessedData, RejectedData, Project, ProjectID, Assignee, TotalData, Date, State) VALUES (@ProcessedData, @RejectedData, @Project, @ProjectID, @Assignee, @TotalData, @Date, @State)";
			string updatequery = "Update [IMS].[dbo].[Data] set ProcessedData = @ProcessedData , RejectedData = @RejectedData , Assignee =  @Assignee , State = @State , Date = @Date , TotalData = @TotalData where ProjectID = @ProjectID";

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				int rowsAffected;
				connection.Open();
				using (SqlCommand checkcmd = new SqlCommand(querycheck, connection))
				{
					checkcmd.Parameters.AddWithValue("@ProjectID", projectID);
					int count = (int)checkcmd.ExecuteScalar();

					if (count > 0)
					{
						using (SqlCommand updatecommand = new SqlCommand(updatequery, connection))
						{
							int totaldata = processedData + rejectedData;
							DateTime currentDateTime = DateTime.Now;
							string formattedDateTime = currentDateTime.ToString("dd-MM-yyyy HH:mm");
							updatecommand.Parameters.AddWithValue("@ProcessedData", processedData);
							updatecommand.Parameters.AddWithValue("@RejectedData", rejectedData);
							updatecommand.Parameters.AddWithValue("@Project", project);
							updatecommand.Parameters.AddWithValue("@ProjectID", projectID);
							updatecommand.Parameters.AddWithValue("@Assignee", Assignee);
							updatecommand.Parameters.AddWithValue("@TotalData", totaldata);
							updatecommand.Parameters.AddWithValue("@Date", currentDateTime);
							updatecommand.Parameters.AddWithValue("@State", State);
							rowsAffected = updatecommand.ExecuteNonQuery();
						}


					}

					else
					{
						using (SqlCommand insertcommand = new SqlCommand(queryinsert, connection))
						{
							int totaldata = processedData + rejectedData;
							DateTime currentDateTime = DateTime.Now;
							string formattedDateTime = currentDateTime.ToString("dd-MM-yyyy HH:mm");
							insertcommand.Parameters.AddWithValue("@ProcessedData", processedData);
							insertcommand.Parameters.AddWithValue("@RejectedData", rejectedData);
							insertcommand.Parameters.AddWithValue("@Project", project);
							insertcommand.Parameters.AddWithValue("@ProjectID", projectID);
							insertcommand.Parameters.AddWithValue("@Assignee", Assignee);
							insertcommand.Parameters.AddWithValue("@TotalData", totaldata);
							insertcommand.Parameters.AddWithValue("@Date", currentDateTime);
							insertcommand.Parameters.AddWithValue("@State", State);
							rowsAffected = insertcommand.ExecuteNonQuery();
						}

					}
				}
				if (rowsAffected > 0)
				{
					return Json(new { success = true });
				}
				else
				{
					return Json(new { success = false, message = "Failed to insert data into the database." });
				}
			}
		}

		/*-----------------------------------------Generate TaskID-----------------------------------------------------------*/


		private int GetNextIDForProject(string project)
		{
			int nextID = 1;
			using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
			{
				con.Open();
				string query = "SELECT MAX(Lot_ID) FROM Tasks WHERE Project = @Project";
				using (SqlCommand cmd = new SqlCommand(query, con))
				{
					cmd.Parameters.AddWithValue("@Project", project);
					var result = cmd.ExecuteScalar();
					if (result != DBNull.Value)
					{
						nextID = Convert.ToInt32(result) + 1;
					}
					else
					{

						nextID = 1;
					}
				}
			}
			return nextID;
		}

		/*----------------------------------------- Display Task detail of a particulat TaskID -----------------------------------------------------------*/

		public IActionResult Details(string id)
		{
			var connectionString = _configuration.GetConnectionString("DefaultConnection");
			var task = new Tasks();
			List<ProjectChange> projectChanges = new List<ProjectChange>();
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				var sql = "SELECT * FROM [IMS].[dbo].[Tasks] WHERE ProjectID = @ProjectID";

				using (var command = new SqlCommand(sql, connection))
				{
					command.Parameters.AddWithValue("@ProjectID", id);

					using (var reader = command.ExecuteReader())
					{
						if (reader.Read())
						{
							task.Summary = reader.GetString(reader.GetOrdinal("Summary"));
							task.Description = reader.GetString(reader.GetOrdinal("Description"));
							task.Project = reader.GetString(reader.GetOrdinal("Project"));
							task.Priority = reader.GetString(reader.GetOrdinal("Priority"));
							task.Status = reader.GetString(reader.GetOrdinal("Status"));
							task.Assignee = reader.GetString(reader.GetOrdinal("Assignee"));
							task.ProjectID = reader.GetString(reader.GetOrdinal("ProjectID"));
							task.EstimatedDays = reader.IsDBNull(reader.GetOrdinal("EstimatedDays")) ? 0 : Convert.ToInt32(reader["EstimatedDays"]);
							task.Flag = reader.GetString(reader.GetOrdinal("Flag"));
							task.Type = reader.GetString(reader.GetOrdinal("Type"));
							task.CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy"));
							string endDateString = reader.GetDateTime("EndDate").ToString();
							task.StartDate = reader.GetDateTime("Created");
							DateTime endDate;
							if (DateTime.TryParse(endDateString, out endDate))
							{
								task.EndDate = endDate;
							}
						}
					}
				}
				var filepath = new List<string>();
				sql = "SELECT DISTINCT FilePath FROM [IMS].[dbo].[TaskFiles] WHERE ProjectId = @ProjectID";
				using (var command = new SqlCommand(sql, connection))
				{
					command.Parameters.AddWithValue("@ProjectID", id);

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							filepath.Add(reader.GetString(reader.GetOrdinal("FilePath")));
						}
					}
				}


				var assignees = new List<string>();
				sql = "SELECT DISTINCT UserName FROM [IMS].[dbo].[UserDetails]";
				using (var command = new SqlCommand(sql, connection))
				{
					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							assignees.Add(reader.GetString(reader.GetOrdinal("UserName")));
						}
					}
				}

				var projects = new List<string>();
				sql = "SELECT DISTINCT Projects FROM [IMS].[dbo].[ID_MASTER]";
				using (var command = new SqlCommand(sql, connection))
				{
					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							projects.Add(reader.GetString(reader.GetOrdinal("Projects")));
						}
					}
				}

				ViewBag.Assignees = assignees;
				ViewBag.Projects = projects;
				ViewBag.Filepath = filepath;
			}

			if (task.Flag == "False")
			{
				return View(task);
			}

			return View(task);
		}

		/*----------------------------------------- Delete the Tasks -----------------------------------------------------------*/

		public async Task<IActionResult> Delete(string id)
		{
			try
			{
				string Assigner = HttpContext.Session.GetString("UserName");
				string projectName = "";
				string assignee = "";
				string projectid = "";
				string createdby = "";
				using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
				{
					con.OpenAsync();
					using (SqlCommand cmd = new SqlCommand("Update [IMS].[dbo].[Tasks] set Flag = 'False' WHERE ProjectID = @ProjectID", con))
					{
						cmd.Parameters.AddWithValue("@ProjectID", id);
						await cmd.ExecuteNonQueryAsync();
					}

					string query = "SELECT * FROM [IMS].[dbo].[Tasks] WHERE ProjectID = @ProjectID";
					using (SqlCommand selectcmd = new SqlCommand(query, con))
					{
						selectcmd.Parameters.AddWithValue("@ProjectID", id);
						using (SqlDataReader reader = selectcmd.ExecuteReader())
						{
							if (reader.Read())
							{
								assignee = reader["Assignee"].ToString();
								projectName = reader["Project"].ToString();
								createdby = reader["CreatedBy"].ToString();
							}
						}
					}

					using (SqlCommand notification = new SqlCommand("INSERT INTO Notifications (Task_ID, UserName, Action_User, Notification_type, Message, Created_at, UserFlag, SA_Flag) VALUES (@ProjectID, @UserName, @ActionUser, @Type, @Message, @Time, @Flag, @Flag)", con))
					{
						notification.Parameters.AddWithValue("@ProjectID", id);
						notification.Parameters.AddWithValue("@UserName", createdby);
						notification.Parameters.AddWithValue("@ActionUser", assignee);
						notification.Parameters.AddWithValue("@Type", "Task Deletion");
						notification.Parameters.AddWithValue("@Message", $"Deleted the task {id} assigned by");
						notification.Parameters.AddWithValue("@Time", DateTime.Now);
						notification.Parameters.AddWithValue("@Flag", "True");

						await notification.ExecuteNonQueryAsync();

					}

					FireAndForgetDeleteEmailSending(id, projectName, Assigner);
					/*List<string> adminEmails = new List<string>();
					using (SqlCommand getEmailCmd = new SqlCommand("SELECT [Email Id] FROM [IMS].[dbo].[UserDetails] WHERE UserRole = 'Admin'", con))
					{
						using (SqlDataReader reader = getEmailCmd.ExecuteReader())
						{
							while (reader.Read())
							{
								adminEmails.Add(reader["Email Id"].ToString());
							}
						}
					}

					string adminEmailsString = string.Join(";", adminEmails);

					string assigner = HttpContext.Session.GetString("UserName");
					string host = _configuration.GetSection("EmailSettings:SmtpHost").Value;
					int port = Convert.ToInt32(_configuration.GetSection("EmailSettings:SmtpPort").Value);
					string sender = _configuration.GetSection("EmailSettings:EmailSender").Value;
					string sender_password = _configuration.GetSection("EmailSettings:EmailPassword").Value;
					string currentDateTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm");
					string subject = "Task Deleted";
					string body = "<!DOCTYPE html>" +
							  "<html lang=\"en\">" +
							  "<head>" +
							  "<meta charset=\"UTF-8\">" +
							  "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
							  "<title>Task Deletion</title>" +
							  "</head>" +
							  "<body>" +
							  "<div style=\"font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;\">" +
							  $"<p style=\"color: #333;\">Dear Admin,</p>" +
							  $"<p style=\"color: #333;\">Task <strong>{id}</strong> in project <strong>{projectName}</strong> was deleted by <strong>{Assigner}</strong>  at {currentDateTime}.</p>" +
							  "<ul style=\"color: #333; list-style-type: none; padding-left: 0;\">" +
							  "</ul>" +
							  "<p style=\"color: #333;\">Best regards,<br>IMS-MIS Team</p>" +
							  "</div>" +
							  "</body>" +
							  "</html>";
					EmailSender.SendEmail(sender, sender_password, host, port, true, adminEmailsString, subject, body);
					*/
				}
			}
			catch (Exception ex)
			{

			}
			return RedirectToAction("Tasks");
		}
		private async Task SendTaskDeletedEmailAsync(string id,SqlConnection con, string projectName, string Assigner)
		{
			List<string> adminEmails = new List<string>();
			using (SqlCommand getEmailCmd = new SqlCommand("SELECT [Email Id] FROM [IMS].[dbo].[UserDetails] WHERE UserRole in('Super Admin','Admin')", con))
			{
				using (SqlDataReader reader = getEmailCmd.ExecuteReader())
				{
					while (reader.Read())
					{
						adminEmails.Add(reader["Email Id"].ToString());
					}
				}
			}

			string adminEmailsString = string.Join(";", adminEmails);

			string assigner = HttpContext.Session.GetString("UserName");
			string host = _configuration.GetSection("EmailSettings:SmtpHost").Value;
			int port = Convert.ToInt32(_configuration.GetSection("EmailSettings:SmtpPort").Value);
			string sender = _configuration.GetSection("EmailSettings:EmailSender").Value;
			string sender_password = _configuration.GetSection("EmailSettings:EmailPassword").Value;
			string currentDateTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm");
			string subject = "Task Deleted";
			string body = "<!DOCTYPE html>" +
					  "<html lang=\"en\">" +
					  "<head>" +
					  "<meta charset=\"UTF-8\">" +
					  "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
					  "<title>Task Deletion</title>" +
					  "</head>" +
					  "<body>" +
					  "<div style=\"font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;\">" +
					  $"<p style=\"color: #333;\">Dear Admin,</p>" +
					  $"<p style=\"color: #333;\">Task <strong>{id}</strong> in project <strong>{projectName}</strong> was deleted by <strong>{Assigner}</strong>  at {currentDateTime}.</p>" +
					  "<ul style=\"color: #333; list-style-type: none; padding-left: 0;\">" +
					  "</ul>" +
					  "<p style=\"color: #333;\">Best regards,<br>IMS-MIS Team</p>" +
					  "</div>" +
					  "</body>" +
					  "</html>";
			EmailSender.SendEmail(sender, sender_password, host, port, true, adminEmailsString, subject, body);

		}
		private void FireAndForgetDeleteEmailSending(string id,string projectName, string Assigner)
		{
			Task.Run(async () =>
			{
				try
				{
					using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
					{
						await con.OpenAsync();
						await SendTaskDeletedEmailAsync(id, con, projectName, Assigner);
						con.Close();
					}

				}
				catch (Exception ex)
				{
					// Log the exception if necessary
					Console.WriteLine("Failed to send email: " + ex.Message);
				}
			});
		}
		/*_______________________________________________redirect_______________________*/
		public IActionResult redirect()
		{
			return RedirectToAction("Tasks");
		}
		/*----------------------------------------- Make changes to the Tasks -----------------------------------------------------------*/

		public IActionResult UpdateTask(Tasks task)
		{
			var connectionString = _configuration.GetConnectionString("DefaultConnection");
			string selectQuery = "SELECT * FROM Tasks WHERE ProjectID = @ProjectID";

			Tasks existingTask = new Tasks();

			string creator = "";
			string user = HttpContext.Session.GetString("UserName");
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				using (SqlCommand command = new SqlCommand(selectQuery, connection))
				{
					command.Parameters.AddWithValue("@ProjectID", task.ProjectID);
					connection.Open();
					SqlDataReader reader = command.ExecuteReader();

					if (reader.HasRows)
					{
						while (reader.Read())
						{
							existingTask.ProjectID = reader["ProjectID"].ToString();
							existingTask.Summary = reader["Summary"].ToString();
							existingTask.Description = reader["Description"].ToString();
							existingTask.Project = reader["Project"].ToString();
							existingTask.Assignee = reader["Assignee"].ToString();
							existingTask.Status = reader["Status"].ToString();
							existingTask.Priority = reader["Priority"].ToString();
							existingTask.EstimatedDays = Convert.ToInt32(reader["EstimatedDays"]);
							existingTask.StartDate = Convert.ToDateTime(reader["Created"]);
							existingTask.EndDate = Convert.ToDateTime(reader["EndDate"]);
							existingTask.Type = reader["Type"].ToString();
							creator = reader["CreatedBy"].ToString();
						}
					}
					reader.Close();
				}
			}


			if (existingTask.Summary != task.Summary && task.Summary != null)
			{
				string updateSummaryQuery = "UPDATE Tasks SET Summary = @Summary WHERE ProjectID = @ProjectID";
				UpdateField(updateSummaryQuery, "@Summary", task.Summary, task.ProjectID);
				UpdateTaskAndLogChange(task.ProjectID, "Summary", existingTask.Summary, task.Summary);
			}

			if (existingTask.Type != task.Type && task.Type != null)
			{
				string updateTypeQuery = "UPDATE Tasks SET Type = @Type WHERE ProjectID = @ProjectID";
				UpdateField(updateTypeQuery, "@Type", task.Type, task.ProjectID);
			}


			if (existingTask.Description != task.Description && task.Description != null)
			{
				string updateDescriptionQuery = "UPDATE Tasks SET Description = @Description WHERE ProjectID = @ProjectID";
				UpdateField(updateDescriptionQuery, "@Description", task.Description, task.ProjectID);
			}

			if (existingTask.Project != task.Project && task.Project != null)
			{
				string updateProjectQuery = "UPDATE Tasks SET Project = @Project WHERE ProjectID = @ProjectID";
				UpdateField(updateProjectQuery, "@Project", task.Project, task.ProjectID);
			}

			if (existingTask.Assignee != task.Assignee && task.Assignee != null)
			{
				string updateAssigneeQuery = "UPDATE Tasks SET Assignee = @Assignee WHERE ProjectID = @ProjectID";
				UpdateField(updateAssigneeQuery, "@Assignee", task.Assignee, task.ProjectID);
				UpdateTaskAndLogChange(task.ProjectID, "Assignee", existingTask.Assignee, task.Assignee);
			}

			if (existingTask.Priority != task.Priority && task.Priority != null)
			{
				string updatePriorityQuery = "UPDATE Tasks SET Priority = @Priority WHERE ProjectID = @ProjectID";
				UpdateField(updatePriorityQuery, "@Priority", task.Priority, task.ProjectID);
				UpdateTaskAndLogChange(task.ProjectID, "Priority", existingTask.Priority, task.Priority);
			}
			if (existingTask.Status == "Done" && task.Status != "Done")
			{
				string updateCompletionDate = "UPDATE Tasks SET CompletionDate = NULL WHERE ProjectID = @ProjectID";
				UpdateField(updateCompletionDate, null, null, task.ProjectID);
			}
			if (existingTask.EstimatedDays != task.EstimatedDays && task.EstimatedDays != null)
			{
				string updateEstimatedDaysQuery = "UPDATE Tasks SET EstimatedDays = @EstimatedDays WHERE ProjectID = @ProjectID";
				UpdateField(updateEstimatedDaysQuery, "@EstimatedDays", task.EstimatedDays.ToString(), task.ProjectID);

				DateTime newEndDate = existingTask.StartDate.AddDays(task.EstimatedDays);
				string updateEndDateQuery = "UPDATE Tasks SET EndDate = @EndDate WHERE ProjectID = @ProjectID";
				UpdateField(updateEndDateQuery, "@EndDate", newEndDate.ToString("yyyy-MM-dd HH:mm:ss"), task.ProjectID);
			}
			if (existingTask.Status != task.Status)
			{
				string updateStatusQuery = "UPDATE Tasks SET Status = @Status WHERE ProjectID = @ProjectID";
				UpdateField(updateStatusQuery, "@Status", task.Status, task.ProjectID);
				UpdateTaskAndLogChange(task.ProjectID, "State", existingTask.Status, task.Status);

				if (task.Status == "Done")
				{
					using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
					{
						var distinctUsers = new List<string>();
						var userEmails = new List<string>();
						con.Open();
						using (SqlCommand users = new SqlCommand("Select UserName, OldValue, NewValue from [IMS].[dbo].[ProjectChanges] where FieldChanged = 'Assignee' and ProjectID = @Projectid", con))
						{
							users.Parameters.AddWithValue("@Projectid", existingTask.ProjectID);
							using (SqlDataReader reader = users.ExecuteReader())
							{
								while (reader.Read())
								{
									string newuser = reader["UserName"].ToString();
									string oldValue = reader["OldValue"].ToString();
									string newValue = reader["NewValue"].ToString();

									if (!string.IsNullOrEmpty(newuser) && !distinctUsers.Contains(newuser))
									{
										distinctUsers.Add(newuser);
									}
									if (!string.IsNullOrEmpty(oldValue) && !distinctUsers.Contains(oldValue))
									{
										distinctUsers.Add(oldValue);
									}
									if (!string.IsNullOrEmpty(newValue) && !distinctUsers.Contains(newValue))
									{
										distinctUsers.Add(newValue);
									}
								}
							}
						}
						using (SqlCommand getUserEmail = new SqlCommand("SELECT [Email Id] FROM [IMS].[dbo].[UserDetails] WHERE UserRole = 'Super Admin'", con))
						{
							using (SqlDataReader reader = getUserEmail.ExecuteReader())
							{
								while (reader.Read())
								{
									string userEmail = reader["Email Id"].ToString();
									if (!string.IsNullOrEmpty(userEmail) && !userEmails.Contains(userEmail))
									{
										userEmails.Add(userEmail);
									}
								}
							}
						}
						foreach (var Users in distinctUsers)
						{
							using (SqlCommand getUserEmail = new SqlCommand("SELECT [Email Id] FROM [IMS].[dbo].[UserDetails] WHERE UserName = @UserName", con))
							{
								getUserEmail.Parameters.AddWithValue("@UserName", Users);
								string userEmail = (string)getUserEmail.ExecuteScalar();
								if (!string.IsNullOrEmpty(userEmail) && !userEmails.Contains(userEmail))
								{
									userEmails.Add(userEmail);
								}
							}

						}
						string userEmailsString = string.Join(";", userEmails);
						string assigner = HttpContext.Session.GetString("UserName");
						string host = _configuration.GetSection("EmailSettings:SmtpHost").Value;
						int port = Convert.ToInt32(_configuration.GetSection("EmailSettings:SmtpPort").Value);
						string sender = _configuration.GetSection("EmailSettings:EmailSender").Value;
						string sender_password = _configuration.GetSection("EmailSettings:EmailPassword").Value;
						string currentDateTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm");
						string subject = "Task Done";
						string body = "<!DOCTYPE html>" +
								  "<html lang=\"en\">" +
								  "<head>" +
								  "<meta charset=\"UTF-8\">" +
								  "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
								  "<title>Task Completion</title>" +
								  "</head>" +
								  "<body>" +
								  "<div style=\"font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;\">" +
								  $"<p style=\"color: #333;\">Dear User,</p>" +
								  $"<p style=\"color: #333;\">Task <strong>{existingTask.ProjectID}</strong> in project <strong>{existingTask.Project}</strong> has been moved to <strong>Done</strong>  by <strong>{user}</strong>  at {currentDateTime}.</p>" +
								  "<ul style=\"color: #333; list-style-type: none; padding-left: 0;\">" +
								  "</ul>" +
								  "<p style=\"color: #333;\">Best regards,<br>IMS-MIS Team</p>" +
								  "</div>" +
								  "</body>" +
								  "</html>";
						EmailSender.SendEmail(sender, sender_password, host, port, true, userEmailsString, subject, body);
					}

					DateTime completionDate = DateTime.Now;
					string updateCompletionDate = "Update Tasks SET CompletionDate = @CompletionDate WHERE ProjectID = @ProjectID";
					UpdateField(updateCompletionDate, "@CompletionDate", completionDate.ToString("yyyy-MM-dd HH:mm:ss"), task.ProjectID);

					if (existingTask.Type == "Data Processing")
					{

						ViewBag.status = "Done";
						ViewBag.Project = existingTask.Project;
						ViewBag.ProjectID = existingTask.ProjectID;
						ViewBag.Assignee = existingTask.Assignee;
						Console.WriteLine(TempData["Status"]);
						return RedirectToAction("Details", "Tasks", new { id = existingTask.ProjectID });
					}


				}
			}
			using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
			{
				con.Open();
				using (SqlCommand notification = new SqlCommand("INSERT INTO Notifications (Task_ID, UserName, Action_User, Notification_type, Message, Created_at, UserFlag, SA_Flag) VALUES (@ProjectID, @UserName, @ActionUser, @Type, @Message, @Time, @Flag, @Flag)", con))
				{
					notification.Parameters.AddWithValue("@ProjectID", existingTask.ProjectID);
					notification.Parameters.AddWithValue("@UserName", creator);
					notification.Parameters.AddWithValue("@ActionUser", user);
					notification.Parameters.AddWithValue("@Type", "Task Updation");
					notification.Parameters.AddWithValue("@Message", $"Updated the task {existingTask.ProjectID} assigned by");
					notification.Parameters.AddWithValue("@Time", DateTime.Now);
					notification.Parameters.AddWithValue("@Flag", "True");

					notification.ExecuteNonQuery();

				}
			}




			return RedirectToAction("Details", "Tasks", new { id = existingTask.ProjectID });
		}

		/*----------------------------------------- Update DB for Changes in Tasks -----------------------------------------------------------*/


		private void UpdateField(string query, string parameterName, string value, string projectId)
		{
			var connectionString = _configuration.GetConnectionString("DefaultConnection");
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				using (SqlCommand command = new SqlCommand(query, connection))
				{
					if (!string.IsNullOrEmpty(value))
					{
						command.Parameters.AddWithValue(parameterName, value);
					}
					command.Parameters.AddWithValue("@ProjectID", projectId);
					connection.Open();
					command.ExecuteNonQuery();
				}
			}
		}


		/*----------------------------------------- Record the changes made in table  -----------------------------------------------------------*/


		private void UpdateTaskAndLogChange(string projectID, string fieldChanged, string oldValue, string newValue)
		{
			string insertQuery = "INSERT INTO ProjectChanges (ProjectID, UserName, FieldChanged, OldValue, NewValue, ChangeTime) VALUES (@ProjectID, @User, @FieldChanged, @OldValue, @NewValue , @DateTime)";

			string user = HttpContext.Session.GetString("UserName");
			using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
			{
				connection.Open();
				using (SqlCommand command = new SqlCommand(insertQuery, connection))
				{
					command.Parameters.AddWithValue("@ProjectID", projectID);
					command.Parameters.AddWithValue("@User", user);
					command.Parameters.AddWithValue("@FieldChanged", fieldChanged);
					command.Parameters.AddWithValue("@OldValue", oldValue);
					command.Parameters.AddWithValue("@NewValue", newValue);
					command.Parameters.AddWithValue("@DateTime", DateTime.Now);
					command.ExecuteNonQuery();
				}
			}
		}


		/*----------------------------------------- Display the deleted Tasks -----------------------------------------------------------*/


		public IActionResult DeletedTasks(int page = 1, int pageSize = 15)
		{
			DataTable dataTable = new DataTable();
			int totalRows = 0;
			int pageCount = 0;

			try
			{
				using (SqlConnection connect = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
				{
					connect.Open();

					using (SqlCommand countCommand = new SqlCommand("SELECT COUNT(*) FROM [IMS].[dbo].[Tasks] where Flag = 'False'", connect))
					{
						totalRows = (int)countCommand.ExecuteScalar();
					}

					pageCount = (int)Math.Ceiling((double)totalRows / pageSize);

					int skip = (page - 1) * pageSize;

					using (SqlCommand commandd = new SqlCommand($"SELECT * FROM [IMS].[dbo].[Tasks] where Flag = 'False' ORDER BY ProjectID OFFSET {skip} ROWS FETCH NEXT {pageSize} ROWS ONLY", connect))
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

			}

			ViewBag.PageCount = pageCount;
			ViewBag.CurrentPage = page;

			return View(dataTable);
		}


		public async Task<IActionResult> Upload(IFormFile file)
		{
			string User = HttpContext.Session.GetString("UserName");
			User = User.Replace(" ", "");
			string uploadFolder = Path.Combine(_webHost.WebRootPath, "Upload", $"Temp_{User}");
			if (!Directory.Exists(uploadFolder))
			{
				Directory.CreateDirectory(uploadFolder);
			}
			string fileName = Path.GetFileName(file.FileName);
			string fileSavePath = Path.Combine(uploadFolder, fileName);

			using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
			{
				await file.CopyToAsync(stream);
			}

			string filePath = Path.Combine(uploadFolder, fileName);
			string fileType = "TaskFile";

			return Json(new { message = fileName + " uploaded successfully!", filePath });
		}


		public async Task<IActionResult> Uploadtaskfile(IFormFile file, string projectID, string project)
		{
			string fileType = "TaskFile";
			string uploadFolder = Path.Combine(_webHost.WebRootPath, "Upload", project, projectID);
			if (!Directory.Exists(uploadFolder))
			{
				Directory.CreateDirectory(uploadFolder);
			}
			string fileName = Path.GetFileName(file.FileName);
			string fileSavePath = Path.Combine(uploadFolder, fileName);
			using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
			{
				await file.CopyToAsync(stream);
			}
			string filePath = Path.Combine(uploadFolder, fileName);
			using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
			{
				con.Open();
				using (SqlCommand filecmd = new SqlCommand("INSERT INTO TaskFiles (Project, ProjectId, FileName, FilePath, FileType) VALUES (@Project, @ProjectID, @FileName, @FilePath, @FileType)", con))
				{
					try
					{
						filecmd.Parameters.AddWithValue("@Project", project);
						filecmd.Parameters.AddWithValue("@ProjectID", projectID);
						filecmd.Parameters.AddWithValue("@FileName", fileName);
						filecmd.Parameters.AddWithValue("@FilePath", filePath);
						filecmd.Parameters.AddWithValue("@FileType", fileType);
						filecmd.ExecuteNonQuery();
					}
					catch (Exception ex)
					{

					}

				}
			}

			return Json(new { message = fileName + " uploaded successfully!", filePath });

		}

		public async Task<IActionResult> Uploadcommentfile(IFormFile file, string projectID, string project)
		{
			string fileType = "CommentFile";
			string uploadFolder = Path.Combine(_webHost.WebRootPath, "Upload", project, projectID);
			if (!Directory.Exists(uploadFolder))
			{
				Directory.CreateDirectory(uploadFolder);
			}
			string fileName = Path.GetFileName(file.FileName);
			string fileSavePath = Path.Combine(uploadFolder, fileName);
			using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
			{
				await file.CopyToAsync(stream);
			}
			string filePath = Path.Combine(uploadFolder, fileName);
			using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
			{
				con.Open();
				using (SqlCommand filecmd = new SqlCommand("INSERT INTO TaskFiles (Project, ProjectId, FileName, FilePath, FileType) VALUES (@Project, @ProjectID, @FileName, @FilePath, @FileType)", con))
				{
					try
					{
						filecmd.Parameters.AddWithValue("@Project", project);
						filecmd.Parameters.AddWithValue("@ProjectID", projectID);
						filecmd.Parameters.AddWithValue("@FileName", fileName);
						filecmd.Parameters.AddWithValue("@FilePath", filePath);
						filecmd.Parameters.AddWithValue("@FileType", fileType);
						filecmd.ExecuteNonQuery();
					}
					catch (Exception ex)
					{

					}

				}
			}

			return Json(new { message = fileName + " uploaded successfully!", filePath });

		}


		public IActionResult DeleteFile(string filename)
		{
			string User = HttpContext.Session.GetString("UserName");
			User = User.Replace(" ", "");
			string uploadFolder = Path.Combine(_webHost.WebRootPath, "Upload", $"Temp_{User}");
			if (!string.IsNullOrEmpty(filename))
			{
				string filePath = Path.Combine(uploadFolder, filename);

				if (System.IO.File.Exists(filePath))
				{
					// Delete the file
					System.IO.File.Delete(filePath);

					return Ok("File deleted successfully.");
				}
				else
				{
					// File not found, return error
					return NotFound("File not found.");
				}
			}
			else
			{
				return BadRequest("Invalid filename.");
			}
		}


		public FileResult DownloadFile(string filePath)
		{

			byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

			string contentType = "application/octet-stream";

			return File(fileBytes, contentType, Path.GetFileName(filePath));
		}

		[HttpGet]
		public ActionResult EditFile(string originalFilename, string changedFilename)
		{
			string User = HttpContext.Session.GetString("UserName");
			User = User.Replace(" ", "");
			try
			{
				string tempFolderPath = Path.Combine(_webHost.WebRootPath, "Upload", $"temp_{User}");

				string originalFilePath = Path.Combine(tempFolderPath, originalFilename);

				// Check if the file exists
				if (System.IO.File.Exists(originalFilePath))
				{
					string newFilePath = Path.Combine(tempFolderPath, changedFilename);

					System.IO.File.Move(originalFilePath, newFilePath);

					return Json(new { success = true, message = "File renamed successfully" });
				}
				else
				{
					return Json(new { success = false, message = "File not found" });
				}
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
			}
		}


		public FileResult DownloadFile1(string filename)
		{
			string User = HttpContext.Session.GetString("UserName");
			User = User.Replace(" ", "");
			string filePath = Path.Combine(_webHost.WebRootPath, "Upload", $"temp_{User}", filename);
			byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

			string contentType = "application/octet-stream";

			return File(fileBytes, contentType, Path.GetFileName(filePath));
		}


		/*----------------------------------------- Display Changes and Comments on Screen -----------------------------------------------------------*/


		[HttpGet]
		public IActionResult ProjectChanges(string projectId)
		{
			List<object> changesAndComments = new List<object>();

			try
			{
				using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
				{
					connection.Open();

					string query = @"
							 SELECT ID , 'Change' AS Type,  UserName, FieldChanged AS Field, OldValue, NewValue, ChangeTime AS Time
							 FROM [IMS].[dbo].[ProjectChanges]
							 WHERE ProjectID = @ProjectID
							 UNION
							 SELECT ID , 'Comment' AS Type, UserName AS UserName, 'Comment' AS Field, CommentText AS OldValue, '' AS NewValue, CommentTime AS Time
							 FROM [IMS].[dbo].[ProjectComments]
							 WHERE ProjectID = @ProjectID
							 ORDER BY Time ASC";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@ProjectID", projectId);

						using (SqlDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								string type = reader.GetString(reader.GetOrdinal("Type"));
								if (type == "Change")
								{
									ProjectChange change = new ProjectChange
									{
										ChangeID = reader.GetInt32("ID"),
										UserName = reader.GetString(reader.GetOrdinal("UserName")),
										Field = reader.GetString(reader.GetOrdinal("Field")),
										OldValue = reader.GetString(reader.GetOrdinal("OldValue")),
										NewValue = reader.GetString(reader.GetOrdinal("NewValue")),
										Time = reader.GetDateTime(reader.GetOrdinal("Time")),
									};
									changesAndComments.Add(change);
								}
								else if (type == "Comment")
								{
									ProjectComment comment = new ProjectComment
									{
										CommentId = reader.GetInt32("ID"),
										UserName = reader.GetString(reader.GetOrdinal("UserName")),
										CommentText = reader.GetString(reader.GetOrdinal("OldValue")),
										Time = reader.GetDateTime(reader.GetOrdinal("Time"))
									};
									changesAndComments.Add(comment);
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			return Json(changesAndComments);
		}


		/*----------------------------------------- Add Comments -----------------------------------------------------------*/


		[HttpPost]
		public IActionResult InsertData([FromBody] CommentData data)
		{
			string connectionString = _configuration.GetConnectionString("DefaultConnection");
			string query = "INSERT INTO [IMS].[dbo].[ProjectComments] (CommentText, ProjectID , UserName, CommentTime) VALUES (@Comment, @ProjectID, @UserName , GETDATE())";
			string user = HttpContext.Session.GetString("UserName");

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				connection.Open();

				try
				{
					using (SqlCommand command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@Comment", data.Comment);
						command.Parameters.AddWithValue("@ProjectID", data.ProjectId);
						command.Parameters.AddWithValue("@UserName", user);
						command.ExecuteNonQuery();
					}

					return Ok("Comment added successfully.");
				}
				catch (SqlException ex)
				{
					return StatusCode(500, $"Internal server error: {ex.Message}");
				}
			}
		}

		[HttpPost]
		public IActionResult DeleteUploadedFile(string filename, string projectID, string project, string fileType)
		{
			try
			{
				string filepath = Path.Combine(_webHost.WebRootPath, "Upload", $"{project}", $"{projectID}", $"{filename}");

				System.IO.File.Delete(filepath);
				string connectionString = _configuration.GetConnectionString("DefaultConnection");
				string query = "Delete from [IMS].[dbo].[TaskFiles] where ProjectId = @ProjectID and FileName = @Filename and FileType= @fileType";
				using (SqlConnection connection = new SqlConnection(connectionString))
				{
					connection.Open();

					try
					{
						using (SqlCommand command = new SqlCommand(query, connection))
						{
							command.Parameters.AddWithValue("@ProjectID", projectID);
							command.Parameters.AddWithValue("@Filename", filename);
							command.Parameters.AddWithValue("@FileType", fileType);
							command.ExecuteNonQuery();
						}

						return Ok("Comment added successfully.");
					}
					catch (SqlException ex)
					{
						return StatusCode(500, $"Internal server error: {ex.Message}");
					}
				}

			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		[HttpPost]
		public IActionResult Deletecomment(string comment, string time, string projectid, string id)
		{
			string user = HttpContext.Session.GetString("UserName");
			string connectionString = _configuration.GetConnectionString("DefaultConnection");
			string query = "Delete from [IMS].[dbo].[ProjectComments] where ProjectId = @ProjectID and ID = @ID";
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				connection.Open();

				try
				{
					using (SqlCommand command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@ProjectID", projectid);
						command.Parameters.AddWithValue("@ID", id);
						command.ExecuteNonQuery();
					}

					return Json(new { success = true });
				}
				catch (SqlException ex)
				{
					return StatusCode(500, $"Internal server error: {ex.Message}");
				}
			}

			return Ok();
		}

		[HttpPost]
		public IActionResult EditComment(string id, string commentText)
		{
			string connectionString = _configuration.GetConnectionString("DefaultConnection");
			string query = "Update [IMS].[dbo].[ProjectComments] set CommentText = @Comment where ID = @ID";
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				connection.Open();
				try
				{
					using (SqlCommand command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@ID", id);
						command.Parameters.AddWithValue("@Comment", commentText);
						command.ExecuteNonQuery();
					}

					return Json(new { success = true, message = "Comment edited successfully." });
				}
				catch (SqlException ex)
				{
					return Json(new { success = false, message = $"Internal server error: {ex.Message}" });
				}
			}
		}




		[HttpPost]
		public IActionResult EditUploadFile(string newFilename, string originalFilename, string project, string projectId, string fileType)
		{
			string folderpath = Path.Combine(_webHost.WebRootPath, "Upload", $"{project}", $"{projectId}");
			string originalFilePath = Path.Combine(folderpath, originalFilename);
			if (System.IO.File.Exists(originalFilePath))
			{
				string newFilePath = Path.Combine(folderpath, newFilename);

				System.IO.File.Move(originalFilePath, newFilePath);
				string connectionString = _configuration.GetConnectionString("DefaultConnection");
				string query = "Update [IMS].[dbo].[TaskFiles] set FileName = @FileName, FilePath = @FilePath where ProjectId = @ProjectID and FileName = @File and FileType = @fileType ";
				using (SqlConnection connection = new SqlConnection(connectionString))
				{
					connection.Open();
					try
					{
						using (SqlCommand command = new SqlCommand(query, connection))
						{
							command.Parameters.AddWithValue("@ProjectID", projectId);
							command.Parameters.AddWithValue("@FileName", newFilename);
							command.Parameters.AddWithValue("@FilePath", newFilePath);
							command.Parameters.AddWithValue("@File", originalFilename);
							command.Parameters.AddWithValue("@fileType", fileType);
							command.ExecuteNonQuery();
						}

						return RedirectToAction("Details", "Tasks", new { id = projectId });
					}
					catch (SqlException ex)
					{
						return StatusCode(500, $"Internal server error: {ex.Message}");
					}
				}

			}

			return Ok();
		}



		[HttpGet]
		public IActionResult GetProjectDataCounts(string projectID)
		{
			try
			{
				// Fetch data from the database
				var dataCounts = new List<ProjectDataCounts>();

				// Database connection and query
				string connectionString = _configuration.GetConnectionString("DefaultConnection");
				string query = "SELECT State, ProcessedData , RejectedData FROM [IMS].[dbo].[Data] WHERE ProjectID = @ProjectID";

				using (SqlConnection connection = new SqlConnection(connectionString))
				{
					connection.Open();

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@ProjectID", projectID);

						using (SqlDataReader reader = command.ExecuteReader())
						{
							// Read data from the reader and populate the list of ProjectDataCounts
							while (reader.Read())
							{
								var stateName = reader.GetString(0); // Assuming StateName is stored in the first column
								var pcount = reader.GetInt32(1); // Assuming Count is stored in the second column
								var rcount = reader.GetInt32(2); // Assuming Count is stored in the second column
								dataCounts.Add(new ProjectDataCounts { StateName = stateName, ProcessedCount = pcount, RejectedCount = rcount });
							}
						}
					}
				}

				return Json(dataCounts);
			}
			catch (SqlException ex)
			{
				return StatusCode(500, $"Internal server error: {ex.Message}");
			}
		}

		public class ProjectDataCounts
		{
			public string StateName { get; set; }
			public int ProcessedCount { get; set; }
			public int RejectedCount { get; set; }
		}


		public class CommentData
		{
			public string Comment { get; set; }
			public string ProjectId { get; set; }
		}

	}
}

