using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Service
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("EmailSettings");
                var smtpClient = new SmtpClient(smtpSettings["SmtpServer"])
                {
                    Port = int.Parse(smtpSettings["SmtpPort"]),
                    Credentials = new NetworkCredential(smtpSettings["SenderEmail"], smtpSettings["SenderPassword"]),
                    EnableSsl = bool.Parse(smtpSettings["EnableSsl"])
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpSettings["SenderEmail"]),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);
                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation($"✅ Email sent to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error sending email: {ex.Message}");
                return false;
            }
        }
    }
}
