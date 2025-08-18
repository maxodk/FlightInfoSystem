using FlightStorageService.Models;
using FlightStorageService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace FlightStorageService.Controllers;

[EnableRateLimiting("flights")]
[ApiController]
[Produces("application/json")]
[SwaggerTag("Запити до БД рейсів (FlightsDb) через збережені процедури.")]
[Route("api/[controller]")]
public sealed class FlightsController : ControllerBase
{
    private readonly IFlightService _svc;
    private readonly ILogger<FlightsController> _log;

    public FlightsController(IFlightService svc, ILogger<FlightsController> log)
    {
        _svc = svc;
        _log = log;
    }

    // GET /api/flights/PS101
    [HttpGet("{flightNumber}")]
    [SwaggerOperation(
            Summary = "Отримати рейс за номером",
            Description = "Використовує збережену процедуру dbo.GetFlightByNumber.")]
    [ProducesResponseType(typeof(Flight), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByNumber(string flightNumber, CancellationToken ct)
    {
        _log.LogInformation("Fetching from DB...");
        var f = await _svc.GetByNumberAsync(flightNumber, ct);
        return f is null ? NotFound() : Ok(f);
    }

    // GET /api/flights?date=2025-08-15
    [HttpGet]
    [SwaggerOperation(
            Summary = "Отримати рейс за датою (UTC)",
            Description = "Використовує збережену процедуру dbo.GetFlightsByDate.")]
    [ProducesResponseType(typeof(IEnumerable<Flight>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByDate([FromQuery] string date, CancellationToken ct)
    {
        return Ok(await _svc.GetByDateAsync(date, ct));
    }
      
    // GET /api/flights/departure?city=Kyiv&date=2025-08-15
    [HttpGet("departure")]
    [SwaggerOperation(
            Summary = "Отримати рейс за містом вильоту + датою",
            Description = "Використовує збережену процедуру dbo.GetFlightsByDepartureCityAndDate.")]
    [ProducesResponseType(typeof(IEnumerable<Flight>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByDeparture([FromQuery] string city, [FromQuery] string date, CancellationToken ct)
    {
        return Ok(await _svc.GetByDepartureAsync(city, date, ct));
    }

    // GET /api/flights/arrival?city=Warsaw&date=2025-08-15
    [HttpGet("arrival")]
    [SwaggerOperation(
            Summary = "Отримати рейс за містом прильоту + датою",
            Description = "Використовує збережену процедуру dbo.GetFlightsByArrivalCityAndDate.")]
    [ProducesResponseType(typeof(IEnumerable<Flight>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByArrival([FromQuery] string city, [FromQuery] string date, CancellationToken ct)
    {
        return Ok(await _svc.GetByArrivalAsync(city, date, ct));
    }
}
