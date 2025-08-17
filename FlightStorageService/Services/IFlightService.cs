using FlightStorageService.Models;

namespace FlightStorageService.Services;

public interface IFlightService
{
    Task<Flight?> GetByNumberAsync(string flightNumber, CancellationToken ct);
    Task<IReadOnlyList<Flight>> GetByDateAsync(string dateIso, CancellationToken ct);
    Task<IReadOnlyList<Flight>> GetByDepartureAsync(string city, string dateIso, CancellationToken ct);
    Task<IReadOnlyList<Flight>> GetByArrivalAsync(string city, string dateIso, CancellationToken ct);
}
