using BuildingBlocks.Messaging.Events.Email;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.CommonConsumer.Email
{
    public class EmailConsumer : IConsumer<EmailEvent>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailConsumer> _logger;

        public EmailConsumer(IConfiguration config, ILogger<EmailConsumer> logger)
        {
            _configuration = config;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<EmailEvent> context)
        {
            var emailEvent = context.Message;

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress
            (_configuration["EmailSettings:SenderName"], _configuration["EmailSettings:SenderEmail"]!));
            email.To.Add(MailboxAddress.Parse(emailEvent.To));
            email.Subject = emailEvent.Subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = emailEvent.Body };

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            try
            {
                await smtp.ConnectAsync(_configuration["EmailSettings:SmtpServer"]!, int.Parse(_configuration["EmailSettings:SmtpPort"]!), MailKit.Security.SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(_configuration["EmailSettings:Username"]!, _configuration["EmailSettings:Password"]!);

                await smtp.SendAsync(email);

                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Email} with subject {Subject} with body : {Body}", emailEvent.To, emailEvent.Subject, emailEvent.Body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email} with subject {Subject}", emailEvent.To, emailEvent.Subject);
            }
        }
    }
}
