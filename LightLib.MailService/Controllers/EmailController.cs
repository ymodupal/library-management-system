using LightLib.MailService.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace LightLib.MailService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        [HttpPost]
        public IActionResult Index(EmailModel model)
        {
            try
            {
                var fromAddress = Environment.GetEnvironmentVariable("FromEmail");
                var mailAccountPwd = Environment.GetEnvironmentVariable("EmailPassword");

                using (MailMessage mm = new MailMessage(fromAddress, model.To))
                {
                    if (model.Action.Equals("CheckIn", StringComparison.OrdinalIgnoreCase))
                    {
                        mm.Subject = "Library - You have checked in an item.";
                        mm.Body = GetCheckinEmailContent(model);
                    }
                    else if (model.Action.Equals("CheckOut", StringComparison.OrdinalIgnoreCase))
                    {
                        mm.Subject = "Library - You have checked out an item.";
                        mm.Body = GetCheckoutEmailContent(model);
                    }
                    else
                    {
                        return BadRequest("Invalid Action");
                    }

                    mm.IsBodyHtml = true;
                    using (SmtpClient smtp = new SmtpClient())
                    {
                        smtp.Host = "smtp.gmail.com";
                        smtp.EnableSsl = true;
                        NetworkCredential NetworkCred = new NetworkCredential(fromAddress, mailAccountPwd);
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = NetworkCred;
                        smtp.Port = 587;
                        smtp.Send(mm);
                    }
                }

                return Ok();
            }
            catch(Exception ex)
            {
                return BadRequest("Failed to Send Email");
            }
        }

        private string GetCheckoutEmailContent(EmailModel model)
        {
            var sb = new StringBuilder();
            var mailContent = System.IO.File.ReadAllText("Templates\\Checkout.html");
            sb.Append(mailContent);
            sb.Replace("{{AssetType}}", model.AssetType);
            sb.Replace("{{AssetTitle}}", model.AssetName);

            return sb.ToString();
        }

        private string GetCheckinEmailContent(EmailModel model)
        {
            var sb = new StringBuilder();
            var mailContent = System.IO.File.ReadAllText("Templates\\Checkin.html");
            sb.Append(mailContent);
            sb.Replace("{{AssetType}}", model.AssetType);
            sb.Replace("{{AssetTitle}}", model.AssetName);

            return sb.ToString();
        }
    }
}
