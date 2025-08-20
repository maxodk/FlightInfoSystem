// Services/KafkaConsumerService.cs
using Confluent.Kafka;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class KafkaConsumerService : BackgroundService
{
    private readonly ILogger<KafkaConsumerService> _log;
    private readonly IConfiguration _cfg;
    private readonly IMemoryCache _cache;

    public KafkaConsumerService(ILogger<KafkaConsumerService> log, IConfiguration cfg, IMemoryCache cache)
    {
        _log = log;
        _cfg = cfg;
        _cache = cache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        var bootstrap = _cfg["Kafka:BootstrapServers"] ?? "kafka:29092";
        var groupId = _cfg["Kafka:GroupId"] ?? "flight-api";
        var topic = _cfg["Kafka:TopicName"] ?? "flights-cache";
        var enabled = bool.TryParse(_cfg["Kafka:Enabled"], out var en) ? en : true;

        _log.LogInformation("KafkaConsumer starting. Enabled={Enabled}, Bootstrap={Bootstrap}, Group={Group}, Topic={Topic}",
            enabled, bootstrap, groupId, topic);

        if (!enabled)
        {
            _log.LogInformation("KafkaConsumer disabled via config.");
            return;
        }

        try
        {
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = bootstrap,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true,
                SocketKeepaliveEnable = true
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
            consumer.Subscribe(topic);

            _log.LogInformation("KafkaConsumer subscribed. Waiting for messages...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(stoppingToken);
                    if (cr?.Message?.Value != null)
                    {
                        _log.LogInformation("Kafka message received: {Value}", cr.Message.Value);

                        if (cr.Message.Value.Trim().Equals("Worker cleaned up successfully", StringComparison.OrdinalIgnoreCase))
                        {
                            if (_cache is MemoryCache memCache)
                            {
                                memCache.Compact(1.0);
                                _log.LogInformation("MemoryCache cleared by Kafka message.");
                            }
                        }
                    }
                }
                catch (ConsumeException ex)
                {
                    _log.LogWarning(ex, "Consume error: {Reason}", ex.Error.Reason);
                    await Task.Delay(2000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                }
            }

            consumer.Close();
            _log.LogInformation("KafkaConsumer stopped.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fatal error in KafkaConsumer. Going idle to keep host alive.");
            while (!stoppingToken.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
