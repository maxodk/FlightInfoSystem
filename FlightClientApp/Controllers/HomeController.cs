using FlightClientApp.Models;
using FlightClientApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlightClientApp.Controllers;

public sealed class HomeController : Controller
{
    private readonly IFlightsApiClient _api;
    private readonly ILogger<HomeController> _log;

    public HomeController(IFlightsApiClient api, ILogger<HomeController> log)
    {
        _api = api;
        _log = log;
    }

    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> ByNumber(string flightNumber, CancellationToken ct)
    {
        return await SafeResults(async () =>
        {
            if (string.IsNullOrWhiteSpace(flightNumber))
            {
                return new List<FlightDto>();
            }

            flightNumber = flightNumber.Trim();
            var flight = await _api.GetByNumberAsync(flightNumber, ct);

            if (flight == null)
            {
                return new List<FlightDto>();
            }

            return new List<FlightDto> { flight };
        });
    }


    [HttpPost]
    public async Task<IActionResult> ByDate(string date, CancellationToken ct)
    { 
         return await SafeResults(() => _api.GetByDateAsync(date, ct)); 
    }

    [HttpPost]
    public async Task<IActionResult> ByDeparture(string city, string date, CancellationToken ct)
    {
        return await SafeResults(() =>
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return Task.FromResult<IReadOnlyList<FlightDto>>(new List<FlightDto>());
            }
            return _api.GetByDepartureAsync(city.Trim(), date, ct);
        });
    }
        

    [HttpPost]
    public async Task<IActionResult> ByArrival(string city, string date, CancellationToken ct)
    {
        return await SafeResults(() =>
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return Task.FromResult<IReadOnlyList<FlightDto>>(new List<FlightDto>());
            } 
            return _api.GetByArrivalAsync(city.Trim(), date, ct);
        });
    }
        

    private async Task<IActionResult> SafeResults(Func<Task<IReadOnlyList<FlightDto>>> action)
    {
        try
        {
            var data = await action();
            ViewBag.Error = null;
            return View("Results", data);
        }
        catch (ApiException apiEx)
        {
            var status = (int)apiEx.StatusCode;
            var title = apiEx.Problem?.Title ?? apiEx.Message;
            var detail = apiEx.Problem?.Detail;
            var trace = apiEx.Problem?.Extensions.TryGetValue("traceId", out var t) == true ? t?.ToString() : null;

            var friendly = status switch
            {
                400 => "Bad request.",
                404 => "Not Found.",
                429 => "Too many requests.",
                500 => "Internal server error.",
                _ => "Unhandled error."
            };

            ViewBag.Error = $"{status} {title}. {friendly}" + (detail is not null ? $" ??????: {detail}" : "") + (trace is not null ? $" (traceId: {trace})" : "");
            return View("Results", Enumerable.Empty<FlightDto>());
        }
        catch (HttpRequestException ex)
        {
            ViewBag.Error = $"API error: {ex.Message}";
            return View("Results", Enumerable.Empty<FlightDto>());
        }
        catch (Exception ex)
        {
            ViewBag.Error = $": {ex.Message}";
            return View("Results", Enumerable.Empty<FlightDto>());
        }
    }

}
