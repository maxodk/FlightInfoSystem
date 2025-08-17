using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace FlightStorageService.Middleware;

public sealed class ProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProblemDetailsMiddleware> _log;

    public ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task Invoke(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            // Map exception -> HTTP status
            var (status, title) = ex switch
            {
                ArgumentException => (HttpStatusCode.BadRequest, "Invalid input"),
                BadHttpRequestException => (HttpStatusCode.BadRequest, "Bad request"),
                KeyNotFoundException => (HttpStatusCode.NotFound, "Not found"),
                _ => (HttpStatusCode.InternalServerError, "Server error")
            };

            // Лог — з трас-ідентифікатором
            _log.LogError(ex, "Unhandled exception at {Path} (TraceId: {TraceId})",
                ctx.Request.Path, ctx.TraceIdentifier);

            var pd = new ProblemDetails
            {
                Status = (int)status,
                Title = title,
                Detail = ex.Message,
                Instance = ctx.Request.Path,
                Type = $"https://httpstatuses.io/{(int)status}"
            };

            // Корисно віддати traceId клієнту
            pd.Extensions["traceId"] = ctx.TraceIdentifier;

            ctx.Response.ContentType = "application/problem+json";
            ctx.Response.StatusCode = pd.Status!.Value;
            await ctx.Response.WriteAsJsonAsync(pd);
        }
    }
}
