using System.Data;
using IMSMIS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace IMSMIS.Controllers
{
	public class UserController : Controller
	{
		private readonly IConfiguration _configuration;
		public UserController(IConfiguration configuration)
		{
			_configuration = configuration;
		}
		public IActionResult Users(int page = 1, int pageSize = 10)
		{

			DataTable dataTable = new DataTable();
			int totalRows = 0;
			int pageCount = 0;
			try
			{
				using (SqlConnection connect = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
				{
					connect.Open();

					using (SqlCommand countCommand = new SqlCommand("SELECT COUNT(*) FROM [IMS].[dbo].[UserDetails]", connect))
					{
						totalRows = (int)countCommand.ExecuteScalar();
					}
					pageCount = (int)Math.Ceiling((double)totalRows / pageSize);
					int skip = (page - 1) * pageSize;
					using (SqlCommand commandd = new SqlCommand($"SELECT * FROM [IMS].[dbo].[UserDetails] ORDER BY UserID OFFSET {skip} ROWS FETCH NEXT {pageSize} ROWS ONLY", connect))
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

		public IActionResult Create(User userDetail)
		{
			try
			{
				string randomPassword = GenerateRandomPassword();
				using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
				{
					con.Open();

					bool emailExists = false;
					using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM [IMS].[dbo].[UserDetails] WHERE [Email Id] = @Email", con))
					{
						checkCmd.Parameters.AddWithValue("@Email", userDetail.EmailId);
						int count = (int)checkCmd.ExecuteScalar();
						emailExists = count > 0;
					}

					if (emailExists)
					{
						TempData["ErrorMessage"] = "Email ID already exists.";
						return RedirectToAction("AddUser");
					}

					else
					{
						using (SqlCommand cmd = new SqlCommand("Create_User", con))
						{
							cmd.CommandType = System.Data.CommandType.StoredProcedure;
							cmd.Parameters.AddWithValue("@Email", userDetail.EmailId);
							cmd.Parameters.AddWithValue("@Password", randomPassword);
							cmd.Parameters.AddWithValue("@Role", userDetail.UserRole);
							cmd.Parameters.AddWithValue("@FirstName", userDetail.FirstName);
							cmd.Parameters.AddWithValue("@LastName", userDetail.LastName);
							cmd.ExecuteReader();

							SendPasswordByEmail(userDetail.EmailId, randomPassword);
						}
					}

				}
			}
			catch (Exception ex)
			{
			}

			return RedirectToAction("Users");
		}


		private string GenerateRandomPassword()
		{
			string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
			Random random = new Random();
			return new string(Enumerable.Repeat(chars, 8)
			  .Select(s => s[random.Next(s.Length)]).ToArray());
		}

		private void SendPasswordByEmail(string recipientEmail, string password)
		{
			string host = _configuration.GetSection("EmailSettings:SmtpHost").Value;
			int port = Convert.ToInt32(_configuration.GetSection("EmailSettings:SmtpPort").Value);
			string sender = _configuration.GetSection("EmailSettings:EmailSender").Value;
			string sender_password = _configuration.GetSection("EmailSettings:EmailPassword").Value;

			string subject = "Your Password for IMS-MIS";
			string body = "<!DOCTYPE html>" +
						  "<html lang=\"en\">" +
						  "<head>" +
						  "<meta charset=\"UTF-8\">" +
						  "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
						  "<title>Login Password Information</title>" +
						  "</head>" +
						  "<body>" +
						  "<div style=\"font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;\">" +
						  "<p style=\"color: #333;\">Dear User,</p>" +
						  $"<p style=\"color: #333;\">We are writing to inform you that your password for accessing our website has been successfully created. Please find your login details below:</p>" +
						  "<ul style=\"color: #333; list-style-type: none; padding-left: 0;\">" +
						  $"<li><strong>Website:</strong> IMS-MIS</li>" +
						  $"<li><strong>Email:</strong> {recipientEmail}</li>" +
						  $"<li><strong>Password:</strong> {password}</li>" +
						  "</ul>" +
						  "<p style=\"color: #333;\">For security reasons, we recommend that you keep your password confidential and avoid sharing it with anyone. If you did not request this password or have any concerns, please contact our support team immediately.</p>" +
						  "<p style=\"color: #333;\">Thank you for choosing IMS-MIS.</p>" +
						  "<p style=\"color: #333;\">Best regards,<br>IMS-MIS Team</p>" +
						  "</div>" +
						  "</body>" +
						  "</html>";

			//EmailSender.SendEmail(sender, sender_password, host, port, true, recipientEmail, subject, body);
		}
		public IActionResult ResetPassword(string emailId)
		{
			try
			{
				string randomPassword = GenerateRandomPassword();
				using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
				{
					con.Open();
					using (SqlCommand cmd = new SqlCommand("UPDATE [IMS].[dbo].[UserDetails] SET [Password] = @Password WHERE [Email Id] = @Email", con))
					{
						cmd.Parameters.AddWithValue("@Password", randomPassword);
						cmd.Parameters.AddWithValue("@Email", emailId);
						cmd.ExecuteNonQuery();

						SendPasswordByEmail(emailId, randomPassword);
					}
				}
			}
			catch (Exception ex)
			{
			}

			return RedirectToAction("Users");
		}

		public IActionResult AddUser()
		{
			return View();
		}
	}
}
