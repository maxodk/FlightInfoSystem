
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<FlightCleanupWorker>();

var host = builder.Build();
host.Run();
