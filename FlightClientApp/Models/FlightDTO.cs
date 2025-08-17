namespace FlightClientApp.Models;

public sealed record FlightDto(
    string FlightNumber,
    DateTime DepartureDateTime,
    string DepartureAirportCity,
    string ArrivalAirportCity,
    int DurationMinutes
);
