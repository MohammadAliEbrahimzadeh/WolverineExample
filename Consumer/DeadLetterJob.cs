using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Consumer;

public class DeadLetterJob : BackgroundService
{
    private readonly ILogger<DeadLetterJob> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public DeadLetterJob(ILogger<DeadLetterJob> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 1. Setup RabbitMQ Connection using the new Async API
        var factory = new ConnectionFactory { Uri = new Uri("amqp://localhost:5672") };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        var queueName = "wolverine-dead-letter-queue";

        // Use AsyncEventingBasicConsumer for v7
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (ch, ea) =>
        {
            var body = ea.Body.ToArray();
            var messagePayload = Encoding.UTF8.GetString(body);

            // Enhanced Exception Headers
            var exceptionType = GetHeader(ea.BasicProperties, "exception-type");
            var exceptionMessage = GetHeader(ea.BasicProperties, "exception-message");

            // Wolverine Envelope Headers
            var originalDestination = GetHeader(ea.BasicProperties, "destination");
            var messageType = GetHeader(ea.BasicProperties, "message-type");
            var sourceApp = GetHeader(ea.BasicProperties, "source");

            var type = ea.BasicProperties.Type;

            _logger.LogError("DLQ Message!\nQueue: {Queue}\nType: {Type}\nSource: {Source}\nException: {ExType} - {ExMsg}\nPayload: {Payload}",
                originalDestination,
                messageType,
                sourceApp,
                exceptionType,
                exceptionMessage,
                messagePayload);

            // TODO: Save to MongoDB or handle it

            // Acknowledge asynchronously
            await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken: stoppingToken);
        };


        await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        // Prevent the background service from exiting immediately
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private string GetHeader(IReadOnlyBasicProperties props, string key)
    {
        if (props.Headers != null && props.Headers.TryGetValue(key, out var value) && value is byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }
        return "Unknown";
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }
}
