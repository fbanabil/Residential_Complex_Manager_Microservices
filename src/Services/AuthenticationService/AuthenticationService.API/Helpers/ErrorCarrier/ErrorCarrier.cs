using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.API.Helpers.ErrorCarrier
{
    public class ErrorCarrier
    {
        public string? Title { get; set; }
        public int StatusCode { get; set; }
        public string? Detail { get; set; }

    }
}
