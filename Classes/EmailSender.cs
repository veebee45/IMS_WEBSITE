using System.Net.Mail;
using System.Net;

namespace IMS_MIS.Classes
{
    public class EmailSender
    {
        public static void SendEmail(string senderEmail, string senderPassword, string smtpHost, int smtpPort, bool enableSSL, string addresses, string subject, string body)
        {
            using (SmtpClient smtpClient = new SmtpClient(smtpHost, smtpPort))
            {
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(senderEmail, senderPassword);
                smtpClient.EnableSsl = enableSSL;
                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(senderEmail);
                foreach (var address in addresses.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    mailMessage.To.Add(address);
                }
                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = true;
                try
                {
                    smtpClient.Send(mailMessage);
                    Console.WriteLine("Email sent successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to send email. Error message: " + ex.Message);
                }
            }
        }


    }
}
