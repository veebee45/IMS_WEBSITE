using IMSMIS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Data;

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
            List<string> assigneereport = new List<string>();
            List<string> projectReport = new List<string>();
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("Select DISTINCT ReportName FROM [IMS].[dbo].[Reports] where Report_Description = 'Issues Per Assignee'", con))
                {
                    using (SqlDataReader assignreport = cmd.ExecuteReader())
                    {
                        while (assignreport.Read())
                        {
                            assigneereport.Add(assignreport.GetString(0));
                        }
                    }
                }

                using (SqlCommand cmd = new SqlCommand("Select DISTINCT ReportName FROM [IMS].[dbo].[Reports] where Report_Description = 'Issues Per Project'", con))
                {
                    using (SqlDataReader projectreport = cmd.ExecuteReader())
                    {
                        while (projectreport.Read())
                        {
                            projectReport.Add(projectreport.GetString(0));
                        }
                    }
                }
            }
            ViewBag.AssigneeReportNames = assigneereport;
            ViewBag.ProjectReportNames = projectReport;
            return View();
        }

        public IActionResult IssueReports()
        {
            List<string> uniqueAssignees = new List<string>();
            List<string> assigneereport = new List<string>();
            List<string> projectReport = new List<string>();
            using (SqlConnection connect = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connect.Open();
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
                using (SqlCommand cmd = new SqlCommand("Select DISTINCT ReportName FROM [IMS].[dbo].[Reports] where Report_Description = 'Issues Per Assignee'", connect))
                {
                    using (SqlDataReader assignreport = cmd.ExecuteReader())
                    {
                        while (assignreport.Read())
                        {
                            assigneereport.Add(assignreport.GetString(0));
                        }
                    }
                }

                using (SqlCommand cmd = new SqlCommand("Select DISTINCT ReportName FROM [IMS].[dbo].[Reports] where Report_Description = 'Issues Per Project'", connect))
                {
                    using (SqlDataReader projectreport = cmd.ExecuteReader())
                    {
                        while (projectreport.Read())
                        {
                            projectReport.Add(projectreport.GetString(0));
                        }
                    }
                }
            }
            ViewBag.AssigneeReportNames = assigneereport;
            ViewBag.ProjectReportNames = projectReport;
            ViewBag.UniqueAssignees = uniqueAssignees;
            return View();
        }



        public IActionResult InsertReportData(ReportData report)
        {
            string type = report.ReportType;
            string creator = HttpContext.Session.GetString("UserName");

			//string reportName = report.ReportName;

			//// Check if the report name already exists
			//bool isDuplicate = CheckForDuplicateReportName(reportName);

			//if (isDuplicate)
			//{
			//	TempData["ErrorMessage"] = "Report name already exists.";
			//	return RedirectToAction("Reports");
			//}


			using (SqlConnection connect = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connect.Open();
                using (SqlCommand issuereport = new SqlCommand("INSERT INTO [IMS].[dbo].[Reports] (ReportName,Report_Description,Filter,ReportType,CreatedBy,CreatedOn) VALUES (@Name,@Description,@Filter,@Type,@Createdby,@date)", connect))
                {
                    issuereport.Parameters.AddWithValue("@Name", report.ReportName);
                    issuereport.Parameters.AddWithValue("@Description", report.ReportDescription);
                    issuereport.Parameters.AddWithValue("@Filter", report.Filter);
                    issuereport.Parameters.AddWithValue("@Type", type);
                    issuereport.Parameters.AddWithValue("@Createdby", creator);
                    issuereport.Parameters.AddWithValue("@date", DateTime.Now);
                    issuereport.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Reports");
        }

		public IActionResult CheckReportName(string reportName)
		{
			bool exists = false;

			// Create a SQL connection
			using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
			{

				connection.Open();

				string query = "SELECT COUNT(*) FROM Reports WHERE ReportName = @ReportName";

				using (SqlCommand command = new SqlCommand(query, connection))
				{

					command.Parameters.AddWithValue("@ReportName", reportName);

					int count = (int)command.ExecuteScalar();

					if (count > 0)
					{
						exists = true;
					}
				}
			}

			return Json(new { exists = exists });
		}


		public IActionResult ProjectReport()
        {
            List<string> uniqueProjects = new List<string>();
            List<string> assigneereport = new List<string>();
            List<string> projectReport = new List<string>();
            using (SqlConnection connect = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connect.Open();
                using (SqlCommand uniqueAssigneesCommand = new SqlCommand("SELECT DISTINCT Projects FROM [IMS].[dbo].[ID_MASTER]", connect))
                {
                    using (SqlDataReader uniqueProjectReader = uniqueAssigneesCommand.ExecuteReader())
                    {
                        while (uniqueProjectReader.Read())
                        {
                            uniqueProjects.Add(uniqueProjectReader.GetString(0));
                        }
                    }
                }
                using (SqlCommand cmd = new SqlCommand("Select DISTINCT ReportName FROM [IMS].[dbo].[Reports] where Report_Description = 'Issues Per Assignee'", connect))
                {
                    using (SqlDataReader assignreport = cmd.ExecuteReader())
                    {
                        while (assignreport.Read())
                        {
                            assigneereport.Add(assignreport.GetString(0));
                        }
                    }
                }

                using (SqlCommand cmd = new SqlCommand("Select DISTINCT ReportName FROM [IMS].[dbo].[Reports] where Report_Description = 'Issues Per Project'", connect))
                {
                    using (SqlDataReader projectreport = cmd.ExecuteReader())
                    {
                        while (projectreport.Read())
                        {
                            projectReport.Add(projectreport.GetString(0));
                        }
                    }
                }
            }
            ViewBag.AssigneeReportNames = assigneereport;
            ViewBag.ProjectReportNames = projectReport;
            ViewBag.UniqueProjects = uniqueProjects;
            return View();
        }


        public IActionResult ReportGraph(string reportName)
        {
            List<TaskData> taskDataList = new List<TaskData>();
            string filter = "";
            string assignee = "";
            string status = "";
            string projects = "";
            string description = "";
            string type = "";
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("Select * From [IMS].[dbo].[Reports] where ReportName = @Name ", con))
                {
                    cmd.Parameters.AddWithValue("@Name", reportName);

                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        filter = reader["Filter"].ToString();
                        description = reader["Report_Description"].ToString();
                        type = reader["ReportType"].ToString().ToLower(); 
                        
                    }
                }
            }
            if (!string.IsNullOrEmpty(filter))
            {
                string[] parts = filter.Split('|');
                foreach (string part in parts)
                {
                    string trimmedPart = part.Trim();

                    if (trimmedPart.StartsWith("Assignee"))
                    {
                        assignee = trimmedPart.Replace("Assignee:", "");
                    }
                    else if (trimmedPart.StartsWith("Status"))
                    {
                        status = trimmedPart.Replace("Status:", "");
                    }
                    else if (trimmedPart.StartsWith("Project"))
                    {
                        projects = trimmedPart.Replace("Project:", "");
                    }
                }
            }


            if (description == "Issues Per Assignee")
            {
				

				// Constructing the SQL query with placeholders for parameters
				string query = "SELECT Assignee, COUNT(*) AS TaskCount FROM [IMS].[dbo].[Tasks] WHERE Assignee IN ({0}) AND Status IN ({1}) GROUP BY Assignee";

				// Formatting the lists of assignees and statuses to be used in the query
				string assigneeParam = string.Join(",", assignee);
				string statusParam = string.Join(",", status);

				// Formatting the placeholders in the query with the actual parameters
				query = string.Format(query, assigneeParam, statusParam);

				using (SqlConnection connect = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
				{
					connect.Open();
					using (SqlCommand command = new SqlCommand(query, connect))
					{
						SqlDataReader reader = command.ExecuteReader();
						while (reader.Read())
						{
							TaskData taskData = new TaskData();
							taskData.Assignee = reader["Assignee"].ToString();
							taskData.TaskCount = Convert.ToInt32(reader["TaskCount"]);
							taskDataList.Add(taskData);
						}
					}
				}
			}
            else if(description == "Issues Per Project")
            {
                string query = "SELECT Project, COUNT(*) AS TaskCount FROM [IMS].[dbo].[Tasks] WHERE Project IN ({0}) AND Status IN ({1}) GROUP BY Project";

                string projectParam = string.Join(",", projects);
                string statusParam = string.Join(",", status);

                query = string.Format (query, projectParam, statusParam);
                using (SqlConnection connect = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connect.Open();
                    using (SqlCommand command = new SqlCommand(query, connect))
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            TaskData taskData = new TaskData();
                            taskData.Project = reader["Project"].ToString();
                            taskData.TaskCount = Convert.ToInt32(reader["TaskCount"]);
                            taskData.Type = type;
                            taskDataList.Add(taskData);
                        }
                    }
                }

            }
            ViewBag.Description = description;
            ViewBag.Type = type;
            ViewBag.TaskDataList = taskDataList;
            return View(taskDataList);
        }




		public IActionResult ReportView()
        {
            List<IMSMIS.Models.Data> dataList = new List<IMSMIS.Models.Data>();
            string query = "SELECT Project, SUM(TotalData) AS TotalCount FROM [IMS].[dbo].[Data] GROUP BY Project";
            List<string> uniqueProjects = new List<string>();
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

                using (SqlCommand uniqueProjectsCommand = new SqlCommand("SELECT DISTINCT Projects FROM [IMS].[dbo].[ID_MASTER]", connection))
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
            ViewBag.UniqueProjects = uniqueProjects;
            return View(dataList);
        }

        public IActionResult ReportTable(int page = 1, int pageSize = 15)
        {
            DataTable dataTable = new DataTable();
            int totalRows = 0;
            int pageCount = 0;
            try
            {
                using (SqlConnection connect = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connect.Open();

                    using (SqlCommand countCommand = new SqlCommand("SELECT COUNT(*) FROM Data", connect))
                    {
                        totalRows = (int)countCommand.ExecuteScalar();
                    }
                    pageCount = (int)Math.Ceiling((double)totalRows / pageSize);
                    int skip = (page - 1) * pageSize;
                    using (SqlCommand commandd = new SqlCommand($"select Project,State,Date,TotalData,ProcessedData,RejectedData,'2' as Developement_Time,'2' as Production_Time,'Production' as Current_Status from [dbo].[Data] order by Project OFFSET {skip} ROWS  FETCH NEXT {pageSize} ROWS ONLY", connect))
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

        [HttpPost]
        public IActionResult ProjectCount(string projectName, string month, int year)
        {
            string project = projectName.Trim();
            List<IMSMIS.Models.Data> statedataList = new List<IMSMIS.Models.Data>();
            string query = "SELECT State, SUM(TotalData) AS TotalCount FROM [IMS].[dbo].[Data] WHERE Project = @Project and DATENAME(MONTH, Date) = @Month and DATEPART(YEAR, Date) = @Year GROUP BY State";

            // Create connection and command objects
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Add parameter to avoid SQL injection
                    command.Parameters.AddWithValue("@Project", project);
                    command.Parameters.AddWithValue("@Month", month);
                    command.Parameters.AddWithValue("@Year", year);

                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        IMSMIS.Models.Data data = new IMSMIS.Models.Data
                        {
                            statename = reader["State"].ToString(),
                            totaldata = (int)reader["TotalCount"]
                        };
                        statedataList.Add(data);
                    }
                }


            }

            return Json(statedataList);
        }


        public ActionResult Project(string projectId, string data)
        {
            // Deserialize the data fetched from the Index action
            List<IMSMIS.Models.Data> stateDataList = JsonConvert.DeserializeObject<List<IMSMIS.Models.Data>>(data);

            // Pass projectId and stateDataList to the Project view
            ViewBag.ProjectId = projectId;

            return View(stateDataList);
        }


        [HttpPost]
        public IActionResult ProjectCount1(string projectName, int? year)
        {
            string project = projectName.Trim();
            int queryYear = year ?? DateTime.Now.Year;
            List<IMSMIS.Models.Data> dataList = new List<IMSMIS.Models.Data>();
            string query = @"
        SELECT 
            DATEPART(YEAR, Date) AS [Year], 
            DATENAME(MONTH, Date) AS [Month], 
            SUM(TotalData) AS TotalCount
        FROM 
            [IMS].[dbo].[Data]
        WHERE 
            Project = @Project
            and  DATEPART(YEAR, Date) = @year
        GROUP BY 
            DATEPART(YEAR, Date), 
            DATENAME(MONTH, Date)
        ORDER BY 
            [Year], 
            [Month]";

            // Create connection and command objects
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Add parameter to avoid SQL injection
                    command.Parameters.AddWithValue("@Project", project);
                    command.Parameters.AddWithValue("@year", queryYear);

                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        IMSMIS.Models.Data data = new IMSMIS.Models.Data
                        {
                            Project = projectName.Trim(),
                            year = (int)reader["Year"],
                            month = reader["Month"].ToString(),
                            totaldata = (int)reader["TotalCount"]
                        };
                        dataList.Add(data);
                    }
                }
            }

            return Json(dataList);
        }


        public ActionResult MonthWise(string projectName, string data)
        {
            // Deserialize the data fetched from the Index action
            List<IMSMIS.Models.Data> stateDataList = JsonConvert.DeserializeObject<List<IMSMIS.Models.Data>>(data);
            List<string> uniqueProjects = new List<string>();
            int year = stateDataList.FirstOrDefault()?.year ?? 0;
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                using (SqlCommand uniqueProjectsCommand = new SqlCommand("SELECT DISTINCT Projects FROM [IMS].[dbo].[ID_MASTER]", connection))
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

            // Pass projectId and stateDataList to the Project view
            ViewBag.Project = projectName;
            ViewBag.Year = year;
            ViewBag.UniqueProjects = uniqueProjects;

            return View(stateDataList);
        }

        public IActionResult ProjectCount2(string projectName, int? year)
        {
            string project = projectName.Trim();
            int queryYear = year ?? DateTime.Now.Year;
            List<IMSMIS.Models.Data> dataList = new List<IMSMIS.Models.Data>();
            string query = @"
        SELECT 
            DATEPART(YEAR, Date) AS [Year], 
            DATENAME(MONTH, Date) AS [Month], 
            SUM(TotalData) AS TotalCount
        FROM 
            [IMS].[dbo].[Data]
        WHERE 
            Project = @Project
            and  DATEPART(YEAR, Date) = @year
        GROUP BY 
            DATEPART(YEAR, Date), 
            DATENAME(MONTH, Date)
        ORDER BY 
            [Year], 
            [Month]";

            // Create connection and command objects
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Add parameter to avoid SQL injection
                    command.Parameters.AddWithValue("@Project", project);
                    command.Parameters.AddWithValue("@year", queryYear);

                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        IMSMIS.Models.Data data = new IMSMIS.Models.Data
                        {
                            Project = projectName.Trim(),
                            year = (int)reader["Year"],
                            month = reader["Month"].ToString(),
                            totaldata = (int)reader["TotalCount"]
                        };
                        dataList.Add(data);
                    }
                }
            }

            return Json(dataList);
        }


    }
}
