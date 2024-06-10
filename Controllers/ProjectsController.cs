using IMSMIS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace IMSMIS.Controllers
{
	public class ProjectsController : Controller
	{
		private readonly IConfiguration _configuration;
		public ProjectsController(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		List<string> uniqueAssignees = new List<string>();
		List<string> uniqueAssignees1 = new List<string>();

		public IActionResult Projects(int page = 1, int pageSize = 10)
		{
			DataTable dataTable = new DataTable();
			int totalRows = 0;
			int pageCount = 0;
			try
			{
				using (SqlConnection connect = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
				{
					connect.Open();

					using (SqlCommand countCommand = new SqlCommand("SELECT COUNT(*) FROM ProjectMaster", connect))
					{
						totalRows = (int)countCommand.ExecuteScalar();
					}
					pageCount = (int)Math.Ceiling((double)totalRows / pageSize);
					int skip = (page - 1) * pageSize;
					using (SqlCommand commandd = new SqlCommand($"SELECT p.id,p.projectid, p.ProjectName, p.ProjectDesc, STRING_AGG(a.AssigneeName, ', ') AS AssigneeNames,p.createdon,p.createdby FROM ProjectMaster p LEFT JOIN ProjectAccessUser a ON p.projectid = a.projectid GROUP BY p.id,p.projectid, p.ProjectName, p.ProjectDesc,p.ProjectDesc,p.createdon,p.createdby  ORDER BY p.projectid  OFFSET {skip} ROWS  FETCH NEXT {pageSize} ROWS ONLY", connect))
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

			ViewBag.PageCount = pageCount;
			ViewBag.CurrentPage = page;


			return View(dataTable);
		}

		public IActionResult AddProject()
		{
			using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
			{
				con.Open();

				using (SqlCommand uniqueAssigneesCommand = new SqlCommand("SELECT DISTINCT UserName FROM [IMS].[dbo].[UserDetails]", con))
				{
					using (SqlDataReader uniqueAssigneesReader = uniqueAssigneesCommand.ExecuteReader())
					{
						while (uniqueAssigneesReader.Read())
						{
							uniqueAssignees.Add(uniqueAssigneesReader.GetString(0));
						}
					}
				}
			}

			ViewBag.UniqueAssignees = uniqueAssignees;

			return View();
		}

		public async Task<IActionResult> Create(Projects projectData, IFormFile uploadFile)
		{
			try
			{
				using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
				{
					con.Open();

					bool emailExists = false;
					using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM [dbo].[ProjectMaster] WHERE [ProjectName] = @ProjectName", con))
					{
						checkCmd.Parameters.AddWithValue("@ProjectName", projectData.ProjectName);
						int count = (int)checkCmd.ExecuteScalar();
						emailExists = count > 0;
					}

					if (emailExists)
					{
						TempData["ErrorMessage"] = "Project already exists.";
						return RedirectToAction("AddProject");
					}

					else
					{
						if (uploadFile != null && uploadFile.Length > 0)
						{
							var filePath = Path.Combine("wwwroot/images", Path.GetFileName(uploadFile.FileName));
							using (var stream = System.IO.File.Create(filePath))
							{
								await uploadFile.CopyToAsync(stream);
							}
							projectData.ImagePath = Path.GetFileName(uploadFile.FileName);
						}

						using (SqlCommand cmd = new SqlCommand("INSERT INTO [dbo].[ProjectMaster] ([ProjectId],[ProjectName],[ProjectDesc],[ProjectExe],[ProjectImg],[CreatedOn],[CreatedBy]) VALUES ('" + projectData.ProjectId + "','" + projectData.ProjectName + "','" + projectData.ProjectDescription + "','" + projectData.ProjectType + "','" + projectData.ImagePath + "', GetDate(), 'Super Admin')", con))
						{
							cmd.ExecuteReader();
						}

						for (int i = 0; i < projectData.Assignees.Count; i++)
						{
							using (SqlCommand cmd = new SqlCommand("INSERT INTO [dbo].[ProjectAccessUser] ([ProjectId],[AssigneeName],[CreatedOn],[CreatedBy]) VALUES ('" + projectData.ProjectId + "','" + projectData.Assignees[i] + "', GetDate(), 'Super Admin')", con))
							{
								cmd.ExecuteReader();
							}
						}
					}

				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			return RedirectToAction("Projects");
		}

		public IActionResult DeleteProject(string projectId)
		{
			try
			{
				using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
				{
					con.Open();
					using (SqlCommand cmd = new SqlCommand("delete from ProjectMaster where ProjectId='" + projectId + "'", con))
					{
						cmd.ExecuteNonQuery();
					}

					using (SqlCommand cmd = new SqlCommand("delete from ProjectAccessUser where ProjectId='" + projectId + "'", con))
					{
						cmd.ExecuteNonQuery();
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			return RedirectToAction("Projects");
		}

		public IActionResult ViewProject(string projectId)
		{
			DataTable dataTable = new DataTable();
			try
			{
				using (SqlConnection connect = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
				{
					connect.Open();

					using (SqlCommand commandd = new SqlCommand($" SELECT p.id,p.projectid, p.ProjectName, p.ProjectDesc, STRING_AGG(a.AssigneeName, ', ') AS AssigneeNames,p.projectexe,p.projectimg,p.createdon,p.createdby FROM ProjectMaster p LEFT JOIN ProjectAccessUser a ON p.projectid = a.projectid where p.projectid ='" + projectId + "' GROUP BY p.id,p.projectid, p.ProjectName,p.projectexe,p.projectimg, p.ProjectDesc,p.createdon,p.createdby ORDER BY  p.projectid ", connect))
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

		public IActionResult EditProject(string projectId)
		{
			using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
			{
				con.Open();

				using (SqlCommand uniqueAssigneesCommand = new SqlCommand("select username from [dbo].[UserDetails] where UserName not in(select [AssigneeName] from [dbo].[ProjectAccessUser] where projectid='" + projectId + "')", con))
				{
					using (SqlDataReader uniqueAssigneesReader = uniqueAssigneesCommand.ExecuteReader())
					{
						while (uniqueAssigneesReader.Read())
						{
							uniqueAssignees1.Add(uniqueAssigneesReader.GetString(0));
						}
					}
				}

				using (SqlCommand commandd = new SqlCommand($"SELECT p.id,p.projectid, p.ProjectName, p.ProjectDesc, STRING_AGG(a.AssigneeName, ', ') AS AssigneeNames,p.projectexe,p.projectimg,p.createdon,p.createdby FROM ProjectMaster p LEFT JOIN ProjectAccessUser a ON p.projectid = a.projectid where p.projectid ='" + projectId + "' GROUP BY p.id,p.projectid, p.ProjectName,p.projectexe,p.projectimg, p.ProjectDesc,p.createdon,p.createdby ORDER BY  p.projectid ", con))
				{
					using (SqlDataReader reader = commandd.ExecuteReader())
					{
						if (reader.Read())
						{
							ViewBag.ProjectId = projectId;
							ViewBag.ProjectName = reader.GetString(2);
							ViewBag.AssigneeNames = reader.GetString(4);
							ViewBag.projectexe = reader.GetString(5);
							ViewBag.projectimg = reader.GetString(6);
						}
					}
				}
			}

			ViewBag.UniqueAssignees1 = uniqueAssignees1;

			return View();
		}

		public async Task<IActionResult> Update(Projects projectData, IFormFile uploadFile)
		{
			try
			{
				using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
				{
					con.Open();

					if (uploadFile != null && uploadFile.Length > 0)
					{
						var filePath = Path.Combine("wwwroot/images", Path.GetFileName(uploadFile.FileName));
						using (var stream = System.IO.File.Create(filePath))
						{
							await uploadFile.CopyToAsync(stream);
						}
						projectData.ImagePath = Path.GetFileName(uploadFile.FileName);


						using (SqlCommand cmd = new SqlCommand("UPDATE [dbo].[ProjectMaster] set [ProjectExe]='" + projectData.ProjectType + "' and [ProjectImg]='" + projectData.ImagePath + "' where ProjectId='" + projectData.ProjectId + "'", con))
						{
							cmd.ExecuteReader();
						}
					}

					for (int i = 0; i < projectData.Assignees.Count; i++)
					{
						using (SqlCommand cmd = new SqlCommand("INSERT INTO [dbo].[ProjectAccessUser] ([ProjectId],[AssigneeName],[CreatedOn],[CreatedBy]) VALUES ('" + projectData.ProjectId + "','" + projectData.Assignees[i] + "', GetDate(), 'Super Admin')", con))
						{
							cmd.ExecuteReader();
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			return RedirectToAction("Projects");
		}
	}
}
