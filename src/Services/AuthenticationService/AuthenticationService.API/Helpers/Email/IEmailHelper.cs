namespace AuthenticationService.API.Helpers.Email
{
    public interface IEmailHelper
    {
        public Task<bool> SendEmail(string emailAddress, string subject, string body);
    }
}
