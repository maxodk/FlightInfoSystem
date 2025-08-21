using FlightStorageService.Caching;
using FlightStorageService.Models;
using FlightStorageService.Repositories;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace FlightStorageService.Services;

public sealed class FlightService : IFlightService
{
    private readonly IFlightRepository _repo;
    private readonly ILogger<FlightService> _log;
    private readonly IAppCache _cache;
    private readonly IHostedService _hostedService;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    public FlightService(IFlightRepository repo,ILogger<FlightService> log,IAppCache cache,IHostedService hostedService)
    {
        _repo = repo;
        _log = log;
        _cache = cache;
        _hostedService = hostedService;
    }

    public async Task<Flight?> GetByNumberAsync(string flightNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(flightNumber))
            throw new ArgumentException("Flight number is required.", nameof(flightNumber));

        var num = flightNumber.Trim();
        var key = $"num:{num.ToUpperInvariant()}";

        var cached = await _cache.GetAsync<Flight>(key, ct);
        if (cached is not null)
        {
            _log.LogInformation("CACHE HIT   {Key}", key);
            return cached;
        }

        _log.LogInformation("CACHE MISS  {Key}", key);
        var res = await _repo.GetByNumberAsync(num, ct);
        if (res is not null)
            await _cache.SetAsync(key, res, CacheTtl, ct);

        return res;
    }

    public async Task<IReadOnlyList<Flight>> GetByDateAsync(string dateIso, CancellationToken ct = default)
    {
        var d = ParseDate(dateIso);
        var key = $"date:{d:yyyy-MM-dd}";

        var cached = await _cache.GetAsync<IReadOnlyList<Flight>>(key, ct);
        if (cached is not null)
        {
            _log.LogInformation("CACHE HIT   {Key}", key);
            return cached;
        }

        _log.LogInformation("CACHE MISS  {Key}", key);
        var res = await _repo.GetByDateAsync(d, ct);
        await _cache.SetAsync(key, res, CacheTtl, ct);
        return res;
    }

    public async Task<IReadOnlyList<Flight>> GetByDepartureAsync(string city, string dateIso, CancellationToken ct = default)
    {
        ValidateCity(city);
        var normCity = city.Trim();
        var d = ParseDate(dateIso);
        var key = $"dep:{normCity.ToLowerInvariant()}:{d:yyyy-MM-dd}";

        var cached = await _cache.GetAsync<IReadOnlyList<Flight>>(key, ct);
        if (cached is not null)
        {
            _log.LogInformation("CACHE HIT   {Key}", key);
            return cached;
        }

        _log.LogInformation("CACHE MISS  {Key}", key);
        var res = await _repo.GetByDepartureCityAndDateAsync(normCity, d, ct);
        await _cache.SetAsync(key, res, CacheTtl, ct);
        return res;
    }

    public async Task<IReadOnlyList<Flight>> GetByArrivalAsync(string city, string dateIso, CancellationToken ct = default)
    {
        ValidateCity(city);
        var normCity = city.Trim();
        var d = ParseDate(dateIso);
        var key = $"arr:{normCity.ToLowerInvariant()}:{d:yyyy-MM-dd}";

        var cached = await _cache.GetAsync<IReadOnlyList<Flight>>(key, ct);
        if (cached is not null)
        {
            _log.LogInformation("CACHE HIT   {Key}", key);
            return cached;
        }

        _log.LogInformation("CACHE MISS  {Key}", key);
        var res = await _repo.GetByArrivalCityAndDateAsync(normCity, d, ct);
        await _cache.SetAsync(key, res, CacheTtl, ct);
        return res;
    }

    private static void ValidateCity(string city)
    {
        if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("City is required.", nameof(city));
        if (city.Length > 100) throw new ArgumentException("City is too long (max 100).", nameof(city));
    }

    private static DateOnly ParseDate(string? dateIso)
    {
        if (string.IsNullOrWhiteSpace(dateIso))
            throw new ArgumentException("Date is required. Expected format: yyyy-MM-dd.", nameof(dateIso));

        if (!DateOnly.TryParseExact(dateIso, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            throw new ArgumentException("Invalid date. Expected format: yyyy-MM-dd.", nameof(dateIso));

        return d;
    }
}
