using Confluent.Kafka;
using KafkaExample.Services;

var builder = Host.CreateApplicationBuilder(args);

var bootstrap = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
builder.Services.AddSingleton(new ProducerConfig
{
    BootstrapServers = bootstrap,
    Acks = Acks.All
});

builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();

builder.Services.AddHostedService<FlightCleanupWorker>();

var host = builder.Build();
await host.RunAsync();
