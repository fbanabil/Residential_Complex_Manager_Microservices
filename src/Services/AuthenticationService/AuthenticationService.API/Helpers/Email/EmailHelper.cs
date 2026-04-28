using AuthenticationService.API.EntityModels;
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

        public Task<bool> SendEmail(string emailAddress, string subject, string body)
        {
            _logger.LogInformation("Simulating sending email to {Email} with subject {Subject} and body {Body}", emailAddress, subject, body);
            return Task.FromResult(true);
        }
    
    }
}
