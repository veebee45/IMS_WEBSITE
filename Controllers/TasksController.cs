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

		public IActionResult Tasks(int page = 1, int pageSize = 10)
		{
			DataTable dataTable = new DataTable();
			int totalRows = 0;
			int pageCount = 0;

			try
			{
				using (SqlConnection connect = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
				{
					connect.Open();
					using (SqlCommand countCommand = new SqlCommand("SELECT COUNT(*) FROM [IMS].[dbo].[Tasks] where Flag = 'True'", connect))
					{
						totalRows = (int)countCommand.ExecuteScalar();
					}

					pageCount = (int)Math.Ceiling((double)totalRows / pageSize);

					int skip = (page - 1) * pageSize;

					using (SqlCommand commandd = new SqlCommand($"SELECT * FROM [IMS].[dbo].[Tasks] where Flag = 'True' ORDER BY Created DESC OFFSET {skip} ROWS FETCH NEXT {pageSize} ROWS ONLY", connect))
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

			ViewBag.PageCount = pageCount;
			ViewBag.CurrentPage = page;
			return View(dataTable);
		}


		/*-----------------------------------------Add New Tasks-----------------------------------------------------------*/


		public IActionResult AddTask()
		{
			List<string> assignees = new List<string>();
			try
			{
				using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
				{
					con.Open();
					using (SqlCommand cmd = new SqlCommand("SELECT UserName FROM  [IMS].[dbo].[UserDetails]", con))
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

			return View();
		}

		/*-----------------------------------------Create the Task-----------------------------------------------------------*/


		[HttpPost]
		public IActionResult SaveTask(Tasks tasks)
		{
			string ProjectID = "";
			try
			{
				string user = HttpContext.Session.GetString("UserName");
				
				using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
				{

					con.Open();
					using (SqlCommand cmd = new SqlCommand("usp_SaveTask", con))
					{
						cmd.CommandType = System.Data.CommandType.StoredProcedure;
						cmd.Parameters.AddWithValue("@Lot_ID", GetNextIDForProject(tasks.Project));
						cmd.Parameters.AddWithValue("@Summary", tasks.Summary);
						cmd.Parameters.AddWithValue("@Description", tasks.Description);
						cmd.Parameters.AddWithValue("@Project", tasks.Project);
						cmd.Parameters.AddWithValue("@Priority", tasks.Priority);
						cmd.Parameters.AddWithValue("@Status", tasks.Status);
						cmd.Parameters.AddWithValue("@Assignee", tasks.Assignee);
						cmd.Parameters.AddWithValue("@Created", tasks.StartDate);
						cmd.Parameters.AddWithValue("@EndDate", tasks.EndDate);
						cmd.Parameters.AddWithValue("@Type", tasks.Type);
						cmd.Parameters.AddWithValue("@EstimatedDays", tasks.EstimatedDays);
						cmd.Parameters.AddWithValue("@CreatedBy", user);

					  ProjectID = tasks.Project + '-' + GetNextIDForProject(tasks.Project);
						string filename = tasks.filenames;
						if (!string.IsNullOrEmpty(filename))
						{
							string[] fileNamesArray = filename.Split(',').Select(f => f.Trim()).ToArray();

							foreach (string fileName in fileNamesArray)
							{
								string User = user.Replace(" ", "");
								string projectFolder = Path.Combine(_webHost.WebRootPath, "Upload", tasks.Project);
								string projectSubFolder = Path.Combine(projectFolder, ProjectID.ToString());
								string tempFilePath = Path.Combine(_webHost.WebRootPath, "Upload", $"temp_{User}", fileName.Trim());
								string targetFilePath = Path.Combine(projectSubFolder, fileName.Trim());

								if (!Directory.Exists(projectFolder))
								{
									Directory.CreateDirectory(projectFolder);
								}

								if (!Directory.Exists(projectSubFolder))
								{
									Directory.CreateDirectory(projectSubFolder);
								}

								System.IO.File.Move(tempFilePath, targetFilePath);

								using (SqlCommand filecmd = new SqlCommand("INSERT INTO TaskFiles (Project, ProjectId, FileName, FilePath) VALUES (@Project, @ProjectID, @FileName, @FilePath)", con))
								{
									string filepath = Path.Combine(projectSubFolder, fileName);
									filecmd.Parameters.AddWithValue("@Project", tasks.Project);
									filecmd.Parameters.AddWithValue("@ProjectID", ProjectID);
									filecmd.Parameters.AddWithValue("@FileName", fileName);
									filecmd.Parameters.AddWithValue("@FilePath", filepath);
									filecmd.ExecuteNonQuery();
								}
							}
						}


						cmd.ExecuteNonQuery();

						string assigneeEmail;
						using (SqlCommand getEmailCmd = new SqlCommand("SELECT [Email Id] FROM [IMS].[dbo].[UserDetails] WHERE UserName = @UserID", con))
						{
							getEmailCmd.Parameters.AddWithValue("@UserID", tasks.Assignee);
							assigneeEmail = (string)getEmailCmd.ExecuteScalar();
						}

						using (SqlCommand notification = new SqlCommand("INSERT INTO Notifications (Task_ID, UserName, Action_User, Notification_type, Message, Created_at, UserFlag, SA_Flag) VALUES (@ProjectID, @UserName, @ActionUser, @Type, @Message, @Time, @Flag, @Flag)", con))
						{
							notification.Parameters.AddWithValue("@ProjectID", ProjectID);
							notification.Parameters.AddWithValue("@UserName", tasks.Assignee);
							notification.Parameters.AddWithValue("@ActionUser", user);
							notification.Parameters.AddWithValue("@Type", "Task Creation");
							notification.Parameters.AddWithValue("@Message", $"created a task {ProjectID} and assigned to");
							notification.Parameters.AddWithValue("@Time", DateTime.Now);
							notification.Parameters.AddWithValue("@Flag", "True");
							notification.ExecuteNonQuery();

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
								  $"<li><strong>Task:</strong> {ProjectID}</li>" +
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
				}
				TempData["SuccessMessage"] = "Task created successfully.";
				return RedirectToAction("Details", "Tasks", new { id = ProjectID });
			}
			catch (Exception ex) {
				TempData["ErrorMessage"] = "Some Error Occured";
			}
			return RedirectToAction("Tasks", "Tasks");
		}

		/*----------------------------------------- Insert Data Count -----------------------------------------------------------*/


		public JsonResult InsertFormData(int processedData, int rejectedData, string project, string projectID, string Assignee, string State)
		{

			string connectionString = _configuration.GetConnectionString("DefaultConnection");

			string query = "INSERT INTO Data (ProcessedData, RejectedData, Project, ProjectID, Assignee, TotalData, Date, State) VALUES (@ProcessedData, @RejectedData, @Project, @ProjectID, @Assignee, @TotalData, @Date, @State)";

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				using (SqlCommand command = new SqlCommand(query, connection))
				{
					int totaldata = processedData + rejectedData;
					DateTime currentDateTime = DateTime.Now;
					string formattedDateTime = currentDateTime.ToString("dd-MM-yyyy HH:mm");
					command.Parameters.AddWithValue("@ProcessedData", processedData);
					command.Parameters.AddWithValue("@RejectedData", rejectedData);
					command.Parameters.AddWithValue("@Project", project);
					command.Parameters.AddWithValue("@ProjectID", projectID);
					command.Parameters.AddWithValue("@Assignee", Assignee);
					command.Parameters.AddWithValue("@TotalData", totaldata);
					command.Parameters.AddWithValue("@Date", currentDateTime);
					command.Parameters.AddWithValue("@State", State);

					connection.Open();
					int rowsAffected = command.ExecuteNonQuery();

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

				ViewBag.Assignees = assignees;
				ViewBag.Filepath = filepath;
			}

			if (task.Flag == "False")
			{
				return View(task);
			}

			return View(task);
		}

		/*----------------------------------------- Delete the Tasks -----------------------------------------------------------*/

		public IActionResult Delete(string id)
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
					con.Open();
					using (SqlCommand cmd = new SqlCommand("Update [IMS].[dbo].[Tasks] set Flag = 'False' WHERE ProjectID = @ProjectID", con))
					{
						cmd.Parameters.AddWithValue("@ProjectID", id);
						cmd.ExecuteNonQuery();
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

						notification.ExecuteNonQuery();

					}

					List<string> adminEmails = new List<string>();
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

				}
			}
			catch (Exception ex)
			{

			}
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
					DateTime completionDate = DateTime.Now;
					string updateCompletionDate = "Update Tasks SET CompletionDate = @CompletionDate WHERE ProjectID = @ProjectID";
					UpdateField(updateCompletionDate, "@CompletionDate", completionDate.ToString("yyyy-MM-dd HH:mm:ss"), task.ProjectID);

					if (existingTask.Type == "Data Processing")
					{

						TempData["Status"] = "Done";
						TempData["Project"] = existingTask.Project;
						TempData["ProjectID"] = existingTask.ProjectID;
						TempData["Assignee"] = existingTask.Assignee;
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


		public IActionResult DeletedTasks(int page = 1, int pageSize = 10)
		{
			DataTable dataTable = new DataTable();
			int totalRows = 0;
			int pageCount = 0;

			try
			{
				using (SqlConnection connect = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
				{
					connect.Open();

					using (SqlCommand countCommand = new SqlCommand("SELECT COUNT(*) FROM [IMS].[dbo].[Tasks] where Flag = 'True'", connect))
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

		//public IActionResult ProjectChanges(string projectId)
		//{
		//	List<ProjectChange> projectChanges = new List<ProjectChange>();

		//	try
		//	{
		//		// Connect to the database
		//		using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
		//		{
		//			connection.Open();

		//			// SQL query to retrieve project changes for the given project ID
		//			string query = "Select UserName , FieldChanged, OldValue, NewValue, ChangeTime FROM [IMS].[dbo].[ProjectChanges] where ProjectID = @ProjectID";

		//			using (SqlCommand command = new SqlCommand(query, connection))
		//			{
		//				// Add parameter to the query
		//				command.Parameters.AddWithValue("@ProjectID", projectId);

		//				using (SqlDataReader reader = command.ExecuteReader())
		//				{
		//					// Read data and populate the list of ProjectChange objects
		//					while (reader.Read())
		//					{
		//						ProjectChange change = new ProjectChange
		//						{
		//							UserName = reader.GetString(0),
		//							Field = reader.GetString(1),
		//							OldValue = reader.GetString(2),
		//							NewValue = reader.GetString(3),
		//							Time = reader.GetDateTime(4)
		//						};
		//						projectChanges.Add(change);
		//					}
		//				}
		//			}
		//		}
		//	}
		//	catch (Exception ex)
		//	{
		//		// Handle exceptions
		//		Console.WriteLine(ex.Message);
		//	}

		//	// Return JSON result
		//	return Json(projectChanges);
		//}

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
		public IActionResult DeleteUploadedFile(string filename, string projectID, string project)
		{
			try
			{
				string filepath = Path.Combine(_webHost.WebRootPath, "Upload", $"{project}", $"{projectID}", $"{filename}");

				System.IO.File.Delete(filepath);
				string connectionString = _configuration.GetConnectionString("DefaultConnection");
				string query = "Delete from [IMS].[dbo].[TaskFiles] where ProjectId = @ProjectID and FileName = @Filename";
				using (SqlConnection connection = new SqlConnection(connectionString))
				{
					connection.Open();

					try
					{
						using (SqlCommand command = new SqlCommand(query, connection))
						{
							command.Parameters.AddWithValue("@ProjectID", projectID);
							command.Parameters.AddWithValue("@Filename", filename);
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
		public IActionResult Deletecomment(string comment , string time, string projectid , string id)
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
		public IActionResult EditComment(string id , string commentText )
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
		public IActionResult EditUploadFile(string newFilename, string originalFilename, string project, string projectId)
		{
			string folderpath = Path.Combine(_webHost.WebRootPath, "Upload", $"{project}", $"{projectId}");
			string originalFilePath = Path.Combine(folderpath, originalFilename);
			if (System.IO.File.Exists(originalFilePath))
			{
				string newFilePath = Path.Combine(folderpath, newFilename);

				System.IO.File.Move(originalFilePath, newFilePath);
				string connectionString = _configuration.GetConnectionString("DefaultConnection");
				string query = "Update [IMS].[dbo].[TaskFiles] set FileName = @FileName, FilePath = @FilePath where ProjectId = @ProjectID and FileName = @File";
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
		public class CommentData
		{
			public string Comment { get; set; }
			public string ProjectId { get; set; }
		}

	}
}

