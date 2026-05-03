using AuthenticationService.API.EntityModels;
using MimeKit;
using System.Security.Permissions;

namespace AuthenticationService.API.Helpers.Email
{
    public class EmailHelper : IEmailHelper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailHelper> _logger;
        public EmailHelper(IConfiguration configuration, ILogger<EmailHelper> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmail(string emailAddress, string subject, string body)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress
            (_configuration["EmailSettings:SenderName"], _configuration["EmailSettings:SenderEmail"]!));
            email.To.Add(MailboxAddress.Parse(emailAddress));
            email.Subject = subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            try
            {
                await smtp.ConnectAsync(_configuration["EmailSettings:SmtpServer"]!, int.Parse(_configuration["EmailSettings:SmtpPort"]!), MailKit.Security.SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(_configuration["EmailSettings:Username"]!, _configuration["EmailSettings:Password"]!);

                await smtp.SendAsync(email);

                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Email} with subject {Subject} with body : {Body}", emailAddress, subject, body);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email} with subject {Subject}", emailAddress, subject);
                return false;
            }
        }
    
    }
}
