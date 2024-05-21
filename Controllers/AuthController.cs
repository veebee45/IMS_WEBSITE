using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using IMSMIS.Models;
using Microsoft.AspNetCore.Http;

namespace IMSMIS.Controllers
{
	public class AuthController : Controller
	{
        private readonly IConfiguration _configuration;
        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Login( User user)
		{

            if (string.IsNullOrEmpty(user.EmailId) || string.IsNullOrEmpty(user.Password))
            {
                ViewBag.ShowIncorrectCredentialsLabel = true;
                ModelState.AddModelError(string.Empty, "Email and Password are required!");
            }
            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("usp_UserLogin", con))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Email", user.EmailId);
                        cmd.Parameters.AddWithValue("@password", user.Password);

                        int result = (int)cmd.ExecuteScalar();
                        if (result == 1)
                        {
                            HttpContext.Session.SetString("UserEmail", user.EmailId);

                            HttpContext.Session.SetString("UserPassword", user.Password);

                            string userRoleQuery = "SELECT UserRole FROM [IMS].[dbo].[UserDetails] WHERE [Email Id] = @Email";
                            using (SqlCommand roleCmd = new SqlCommand(userRoleQuery, con))
                            {
                                roleCmd.Parameters.AddWithValue("@Email", user.EmailId);
                                string userRole = (string)roleCmd.ExecuteScalar();
                                HttpContext.Session.SetString("UserRole", userRole);
                            }

                            string userQuery = "SELECT UserName FROM [IMS].[dbo].[UserDetails] WHERE [Email Id] = @Email";
                            using (SqlCommand roleCmd = new SqlCommand(userQuery, con))
                            {
                                roleCmd.Parameters.AddWithValue("@Email", user.EmailId);
                                string UserName = (string)roleCmd.ExecuteScalar();
                                HttpContext.Session.SetString("UserName", UserName);
                            }
                            
                                return RedirectToAction("Dashboard", "Dashboard");

                        }
                        else
                        {

                            ViewBag.ShowIncorrectCredentialsLabel = true;
                            ModelState.AddModelError(string.Empty, "Invalid Email Id & Password!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                ModelState.AddModelError(string.Empty, "An error occurred while processing your request.");
            }

            return View("Login", user);
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Logout()
        {

            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }
    }
}
