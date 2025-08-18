using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

public class FlightCleanupWorker : BackgroundService
{
    private readonly ILogger<FlightCleanupWorker> _logger;
    private readonly IConfiguration _config;

    public FlightCleanupWorker(ILogger<FlightCleanupWorker> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                string connStr = _config.GetConnectionString("FlightsDB");

                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand("EXEC dbo.CleanupOldFlights", conn))
                {
                    await conn.OpenAsync(stoppingToken);
                    await cmd.ExecuteNonQueryAsync(stoppingToken);
                }

                _logger.LogInformation("CleanupOldFlights executed at: {time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup job");
            }
            int hours = _config.GetValue<int>("WorkerSettings:CleanupIntervalHours");
            await Task.Delay(TimeSpan.FromHours(hours), stoppingToken);

        }
    }
}
