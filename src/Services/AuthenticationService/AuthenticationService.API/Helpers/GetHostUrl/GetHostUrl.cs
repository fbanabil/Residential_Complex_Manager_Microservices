namespace AuthenticationService.API.Helpers.GetHostUrl
{
    public class GetHostUrl : IGetHostUrl
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public GetHostUrl(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public Task<string> GetHostUrlAsync()
        {
            string hostUrl = $"{_contextAccessor?.HttpContext?.Request.Scheme}://{_contextAccessor?.HttpContext?.Request.Host}";
            return Task.FromResult(hostUrl);
        }
    }
}
