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
        _api = api; _log = log;
    }

    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> ByNumber(string flightNumber, CancellationToken ct)
        => await SafeResults(async () =>
        {
            if (string.IsNullOrWhiteSpace(flightNumber))
                return new List<FlightDto>();
            var f = await _api.GetByNumberAsync(flightNumber.Trim(), ct);
            return f is null ? new List<FlightDto>() : new List<FlightDto> { f };
        });

    [HttpPost]
    public async Task<IActionResult> ByDate(string date, CancellationToken ct)
        => await SafeResults(() => _api.GetByDateAsync(date, ct));

    [HttpPost]
    public async Task<IActionResult> ByDeparture(string city, string date, CancellationToken ct)
        => await SafeResults(() =>
        {
            if (string.IsNullOrWhiteSpace(city)) return Task.FromResult<IReadOnlyList<FlightDto>>(new List<FlightDto>());
            return _api.GetByDepartureAsync(city.Trim(), date, ct);
        });

    [HttpPost]
    public async Task<IActionResult> ByArrival(string city, string date, CancellationToken ct)
        => await SafeResults(() =>
        {
            if (string.IsNullOrWhiteSpace(city)) return Task.FromResult<IReadOnlyList<FlightDto>>(new List<FlightDto>());
            return _api.GetByArrivalAsync(city.Trim(), date, ct);
        });

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

            // user-friendly ????????????
            var friendly = status switch
            {
                400 => "????????? ??????????? ???????? ?????.",
                404 => "?? ????? ??????? ?????? ?? ????????.",
                429 => "???????? ???????. ????????? ?? ??? ????? ???????.",
                500 => "?? ??????? ??????? ???????. ????????? ???????.",
                _ => "??????? ??????? ??? ????????? ??????."
            };

            // ??????????? ????? ??? UI
            ViewBag.Error = $"{status} {title}. {friendly}" + (detail is not null ? $" ??????: {detail}" : "") + (trace is not null ? $" (traceId: {trace})" : "");
            return View("Results", Enumerable.Empty<FlightDto>());
        }
        catch (HttpRequestException ex)
        {
            ViewBag.Error = $"API ???????????: {ex.Message}";
            return View("Results", Enumerable.Empty<FlightDto>());
        }
        catch (Exception ex)
        {
            ViewBag.Error = $"??????????? ???????: {ex.Message}";
            return View("Results", Enumerable.Empty<FlightDto>());
        }
    }

}
