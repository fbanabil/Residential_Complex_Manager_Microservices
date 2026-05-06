using BuildingBlocks.Messaging.Events.Email;
using MassTransit;

namespace AuthenticationService.API.Helpers.Email
{
    public class EmailHelper : IEmailHelper
    {
        public readonly IPublishEndpoint _publish;
        public EmailHelper(IPublishEndpoint publish)
        {
            _publish = publish;
        }

        public async Task<bool> SendEmail(string emailAddress, string subject, string body)
        {
            try
            {
                EmailEvent emailEvent = new EmailEvent
                {
                    To = emailAddress,
                    Subject = subject,
                    Body = body
                };

                await _publish.Publish(emailEvent);

                return true;
            }
            catch
            {
                return false;
            }

        }
    
    }
}
