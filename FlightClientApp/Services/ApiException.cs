using System.Net;
using FlightClientApp.Models;

namespace FlightClientApp.Services;

public sealed class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public ProblemDetailsDto? Problem { get; }

    public ApiException(HttpStatusCode statusCode, ProblemDetailsDto? problem, string? fallbackMessage = null)
        : base(problem?.Detail ?? problem?.Title ?? fallbackMessage ?? $"API error {(int)statusCode}")
    {
        StatusCode = statusCode;
        Problem = problem;
    }
}
