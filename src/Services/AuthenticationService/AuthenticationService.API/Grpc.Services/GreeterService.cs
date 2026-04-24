using AuthenticationService.API.Grpc;
using Grpc.Core;

namespace AuthenticationService.API.Grpc.Services
{
    public class GreeterService : Greeter.GreeterBase
    {
        public override Task<HelloReply> SayHello(
        HelloRequest request,
        ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = $"Hello, {request.Name}"
            });
        }
    }
}
