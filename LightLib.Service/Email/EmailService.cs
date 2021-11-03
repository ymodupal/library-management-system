using LightLib.Models.Email;
using LightLib.Service.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace LightLib.Service.Email
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public async Task Send(EmailModel model)
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
                        _logger.LogCritical("Invalid Action in Send Email Service.");
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
                        await smtp.SendMailAsync(mm).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Send Email service.");
            }
        }

        private string GetCheckoutEmailContent(EmailModel model)
        {
            var sb = new StringBuilder();
            var mailContent = System.IO.File.ReadAllText("./Templates/Checkout.html");
            sb.Append(mailContent);
            sb.Replace("{{AssetType}}", model.AssetType);
            sb.Replace("{{AssetTitle}}", model.AssetName);
            sb.Replace("{{CheckoutDate}}", DateTime.Now.Date.ToString("dd/MM/yyyy"));
            sb.Replace("{{DueDate}}", DateTime.Now.AddDays(30).Date.ToString("dd/MM/yyyy"));
            sb.Replace("{{PatronName}}", model.PatronName);

            return sb.ToString();
        }

        private string GetCheckinEmailContent(EmailModel model)
        {
            var sb = new StringBuilder();
            var mailContent = System.IO.File.ReadAllText("./Templates/Checkin.html");
            sb.Append(mailContent);
            sb.Replace("{{AssetType}}", model.AssetType);
            sb.Replace("{{AssetTitle}}", model.AssetName);
            sb.Replace("{{CheckinDate}}", DateTime.Now.Date.ToString("dd/MM/yyyy"));
            sb.Replace("{{PatronName}}", model.PatronName);

            return sb.ToString();
        }
    }
}
