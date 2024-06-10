using IMSMIS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMSMIS.Controllers
{
    public class DashboardController : Controller
    {
		private readonly IConfiguration _configuration;
		public DashboardController(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public IActionResult Dashboard(List<string> projects = null, List<string> statuses = null)
        {
            var viewModel = new DashboardViewModel();

			List<string> uniqueProjects = new List<string>();

			try
            {
                using (SqlConnection connect = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connect.Open();

                    // First DataTable
                    viewModel.DataTable1 = GetData(connect, "usp_dashboard_counter");

                    // Second DataTable
                    viewModel.DataTable2 = GetData(connect, "Top_5_userWiseList");

                    // Third DataTable
                    viewModel.DataTable3 = GetData(connect, "Top_5_ProjectWiseList");

					viewModel.ListData = GetListData(connect, "SELECT top 5 Project, SUM(TotalData) AS TotalCount FROM [Data] GROUP BY Project");

                    viewModel.ListData1= GetListData1(connect, "GetTeamData");

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
                Console.WriteLine(ex);
            }

			ViewBag.UniqueProjects = uniqueProjects;
			ViewBag.SelectedStatuses = statuses;
			ViewBag.SelectedProjects = projects;

			return View(viewModel);

        }

		public void GetCounter()
		{
            DataTable dataTable = new DataTable();

            try
            {
                using (SqlConnection connect = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connect.Open();

                    using (SqlCommand commandd = new SqlCommand("usp_dashboard_counter", connect))
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
                Console.WriteLine(ex);
            }
        }

        private DataTable GetData(SqlConnection connect, string storedProcedure)
        {
            DataTable dataTable = new DataTable();

            using (SqlCommand command = new SqlCommand(storedProcedure, connect))
            {
                command.CommandType = CommandType.StoredProcedure;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
            }

            return dataTable;
        }

		private List<IMSMIS.Models.Data> GetListData(SqlConnection connect, string storedProcedure)
		{
			List<IMSMIS.Models.Data> dataList = new List<IMSMIS.Models.Data>();

            using (SqlCommand command = new SqlCommand(storedProcedure, connect))
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

			return dataList;
		}

        private List<IMSMIS.Models.Data1> GetListData1(SqlConnection connect, string storedProcedure)
        {
            List<IMSMIS.Models.Data1> dataList1 = new List<IMSMIS.Models.Data1>();

            using (SqlCommand command = new SqlCommand(storedProcedure, connect))
            {
                command.CommandType=CommandType.StoredProcedure;

                SqlDataReader reader = command.ExecuteReader();

                // Read the data and populate the list
                while (reader.Read())
                {
                    IMSMIS.Models.Data1 data1 = new IMSMIS.Models.Data1();
                    data1.TeamName = reader["UserRole"].ToString();
                    data1.PendingTask = (int)reader["TaskCount"];
                    dataList1.Add(data1);
                }
            }

            return dataList1;
        }
    }
}
