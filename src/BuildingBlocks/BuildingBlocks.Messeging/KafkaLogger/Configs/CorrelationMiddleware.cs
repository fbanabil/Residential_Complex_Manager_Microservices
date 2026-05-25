using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.KafkaLogger.Configs
{
    public class CorrelationMiddleware
    {
        private const string HeaderName = "X-Correlation-ID";

        private readonly RequestDelegate _next;

        public CorrelationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ILogger<CorrelationMiddleware> logger)
        {
            var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var value)
            ? value.ToString()
            : context.TraceIdentifier;

            context.Response.Headers[HeaderName] = correlationId;

            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            var userId = userIdClaim?.Value ?? "Anonymous";
            var userRoles = context.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
            var userName = context.User.Identity?.Name ?? "Unknown";

            var scopeData = new Dictionary<string, object?>
            {
                ["CorrelationId"] = correlationId,
                ["RequestId"] = context.TraceIdentifier,
                ["Method"] = context.Request.Method,
                ["Path"] = context.Request.Path.Value,
                ["UserId"] = userId,
                ["UserRoles"] = string.Join(",", userRoles),
                ["UserName"] = userName
            };

            using var scope = logger.BeginScope(scopeData);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _next(context);

                stopwatch.Stop();

                logger.LogInformation(
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                logger.LogError(
                    ex,
                    "HTTP {Method} {Path} failed in {ElapsedMs} ms",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds);

                throw;
            }
        }
    }
}
