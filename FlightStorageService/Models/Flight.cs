namespace FlightStorageService.Models;

public sealed record Flight(
    string FlightNumber,
    DateTime DepartureDateTime,
    string DepartureAirportCity,
    string ArrivalAirportCity,
    int DurationMinutes
);
