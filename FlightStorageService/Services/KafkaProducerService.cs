using Confluent.Kafka;

namespace KafkaExample.Services;

public interface IKafkaProducerService
{
    Task SendMessageAsync(string topic, string message);
}

public class KafkaProducerService : IKafkaProducerService
{
    private readonly IProducer<Null, string> _producer;
    public KafkaProducerService(IConfiguration configuration)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration.GetValue<string>("Kafka:BootstrapServers")
        };
        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    // Method to send message to Kafka topic
    public async Task SendMessageAsync(string topic, string message)
    {
        try
        {
            // Send message to the specified Kafka topic
            await _producer.ProduceAsync(topic, new Message<Null, string> { Value = message });
            Console.WriteLine($"Message '{message}' sent to topic '{topic}'.");
        }
        catch (Exception ex)
        {
            // Log any errors encountered while sending message
            Console.WriteLine($"Error sending message to Kafka: {ex.Message}");
            throw;
        }
    }
}