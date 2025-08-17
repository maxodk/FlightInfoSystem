using FlightStorageService.Models;

namespace FlightStorageService.Repositories;

public interface IFlightRepository
{
    Task<Flight?> GetByNumberAsync(string flightNumber, CancellationToken ct);
    Task<IReadOnlyList<Flight>> GetByDateAsync(DateOnly date, CancellationToken ct);
    Task<IReadOnlyList<Flight>> GetByDepartureCityAndDateAsync(string city, DateOnly date, CancellationToken ct);
    Task<IReadOnlyList<Flight>> GetByArrivalCityAndDateAsync(string city, DateOnly date, CancellationToken ct);
}
