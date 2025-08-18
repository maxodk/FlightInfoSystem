using System.Net;
using System.Net.Http.Json;
using FlightClientApp.Models;

namespace FlightClientApp.Services;

public sealed class FlightsApiClient : IFlightsApiClient
{
    private readonly HttpClient _http;
    public FlightsApiClient(HttpClient http) => _http = http;

    public async Task<FlightDto?> GetByNumberAsync(string number, CancellationToken ct = default)
    {
        return await SendAndReadAsync<FlightDto>($"api/flights/{Uri.EscapeDataString(number)}", ct);
    }

    public async Task<IReadOnlyList<FlightDto>> GetByDateAsync(string dateIso, CancellationToken ct = default)
    {
        return await SendAndReadAsync<List<FlightDto>>($"api/flights?date={Uri.EscapeDataString(dateIso)}", ct) ?? [];
    }

    public async Task<IReadOnlyList<FlightDto>> GetByDepartureAsync(string city, string dateIso, CancellationToken ct = default)
    {
        return await SendAndReadAsync<List<FlightDto>>($"api/flights/departure?city={Uri.EscapeDataString(city)}&date={Uri.EscapeDataString(dateIso)}", ct) ?? [];
    }
     

    public async Task<IReadOnlyList<FlightDto>> GetByArrivalAsync(string city, string dateIso, CancellationToken ct = default)
    {
        return await SendAndReadAsync<List<FlightDto>>($"api/flights/arrival?city={Uri.EscapeDataString(city)}&date={Uri.EscapeDataString(dateIso)}", ct) ?? [];
    }

    private async Task<T?> SendAndReadAsync<T>(string url, CancellationToken ct)
    {
        using var resp = await _http.GetAsync(url, ct);

        if (resp.IsSuccessStatusCode)
        {
            return await resp.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        }
            
        ProblemDetailsDto? problem = null;
        try
        {
            problem = await resp.Content.ReadFromJsonAsync<ProblemDetailsDto>(cancellationToken: ct);
        }
        catch {}

        throw new ApiException(resp.StatusCode, problem);
    }
}
