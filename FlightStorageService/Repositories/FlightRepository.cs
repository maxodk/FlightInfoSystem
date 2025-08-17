using System.Data;
using Microsoft.Data.SqlClient;
using FlightStorageService.Models;

namespace FlightStorageService.Repositories;

public sealed class FlightRepository : IFlightRepository
{
    private readonly string _cs;
    public FlightRepository(IConfiguration cfg) => _cs = cfg.GetConnectionString("FlightsDb")!;

    public async Task<Flight?> GetByNumberAsync(string flightNumber, CancellationToken ct)
    {
        await using var con = new SqlConnection(_cs);
        await using var cmd = new SqlCommand("dbo.GetFlightByNumber", con)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.Add(new SqlParameter("@FlightNumber", SqlDbType.NVarChar, 10) { Value = flightNumber });

        await con.OpenAsync(ct);
        await using var rdr = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection, ct);
        if (!await rdr.ReadAsync(ct)) return null;
        return Map(rdr);
    }

    public Task<IReadOnlyList<Flight>> GetByDateAsync(DateOnly date, CancellationToken ct) =>
        QueryListAsync("dbo.GetFlightsByDate",
            new SqlParameter("@Date", SqlDbType.Date) { Value = date.ToDateTime(TimeOnly.MinValue) }, ct);

    public Task<IReadOnlyList<Flight>> GetByDepartureCityAndDateAsync(string city, DateOnly date, CancellationToken ct) =>
        QueryListAsync("dbo.GetFlightsByDepartureCityAndDate", new[]
        {
            new SqlParameter("@City", SqlDbType.NVarChar, 100){ Value = city },
            new SqlParameter("@Date", SqlDbType.Date){ Value = date.ToDateTime(TimeOnly.MinValue) }
        }, ct);

    public Task<IReadOnlyList<Flight>> GetByArrivalCityAndDateAsync(string city, DateOnly date, CancellationToken ct) =>
        QueryListAsync("dbo.GetFlightsByArrivalCityAndDate", new[]
        {
            new SqlParameter("@City", SqlDbType.NVarChar, 100){ Value = city },
            new SqlParameter("@Date", SqlDbType.Date){ Value = date.ToDateTime(TimeOnly.MinValue) }
        }, ct);

    private async Task<IReadOnlyList<Flight>> QueryListAsync(string sp, SqlParameter param, CancellationToken ct) =>
        await QueryListAsync(sp, new[] { param }, ct);

    private async Task<IReadOnlyList<Flight>> QueryListAsync(string sp, SqlParameter[] parameters, CancellationToken ct)
    {
        var list = new List<Flight>();
        await using var con = new SqlConnection(_cs);
        await using var cmd = new SqlCommand(sp, con) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddRange(parameters);

        await con.OpenAsync(ct);
        await using var rdr = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection, ct);
        while (await rdr.ReadAsync(ct))
            list.Add(Map(rdr));
        return list;
    }

    private static Flight Map(SqlDataReader r) => new(
        FlightNumber: r.GetString(r.GetOrdinal("FlightNumber")),
        DepartureDateTime: r.GetDateTime(r.GetOrdinal("DepartureDateTime")),
        DepartureAirportCity: r.GetString(r.GetOrdinal("DepartureAirportCity")),
        ArrivalAirportCity: r.GetString(r.GetOrdinal("ArrivalAirportCity")),
        DurationMinutes: r.GetInt32(r.GetOrdinal("DurationMinutes"))
    );
}
