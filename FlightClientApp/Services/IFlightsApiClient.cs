using FlightClientApp.Models;

namespace FlightClientApp.Services;

public interface IFlightsApiClient
{
    Task<FlightDto?> GetByNumberAsync(string number, CancellationToken ct = default);
    Task<IReadOnlyList<FlightDto>> GetByDateAsync(string dateIso, CancellationToken ct = default);
    Task<IReadOnlyList<FlightDto>> GetByDepartureAsync(string city, string dateIso, CancellationToken ct = default);
    Task<IReadOnlyList<FlightDto>> GetByArrivalAsync(string city, string dateIso, CancellationToken ct = default);
}
