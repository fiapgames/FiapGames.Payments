using System.Text.Json;
using FiapGames.Contracts.IntegrationEvents;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FiapGames.Payments;

public class PaymentWorker(ILogger<PaymentWorker> logger, IOptions<RabbitMqOptions> options) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;
    private IChannel? _channel;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            VirtualHost = _options.VirtualHost,
            UserName = _options.UserName,
            Password = _options.Password
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(_options.OrderPlacedQueue, durable: true, exclusive: false,
            autoDelete: false, cancellationToken: cancellationToken);
        await _channel.QueueDeclareAsync(_options.PaymentProcessedCatalogQueue, durable: true, exclusive: false,
            autoDelete: false, cancellationToken: cancellationToken);
        await _channel.QueueDeclareAsync(_options.PaymentProcessedNotificationsQueue, durable: true, exclusive: false,
            autoDelete: false, cancellationToken: cancellationToken);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel is null)
            throw new InvalidOperationException("RabbitMQ channel was not initialized.");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnOrderPlacedAsync;

        await _channel.BasicConsumeAsync(_options.OrderPlacedQueue, autoAck: false, consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });
    }

    private async Task OnOrderPlacedAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        var channel = (IChannel)((AsyncEventingBasicConsumer)sender).Channel;

        try
        {
            var orderPlaced = JsonSerializer.Deserialize<OrderPlacedEvent>(eventArgs.Body.Span);
            if (orderPlaced is null)
            {
                logger.LogWarning("Failed to deserialize OrderPlacedEvent, message will be discarded.");
                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
                return;
            }

            var status = Random.Shared.Next(2) == 0 ? PaymentStatus.Approved : PaymentStatus.Rejected;

            var paymentProcessed = new PaymentProcessedEvent(
                orderPlaced.OrderId,
                orderPlaced.UserId,
                orderPlaced.GameId,
                status,
                DateTime.UtcNow);

            logger.LogInformation("Payment for order {OrderId} processed with status {Status}",
                paymentProcessed.OrderId, paymentProcessed.Status);

            var payload = JsonSerializer.SerializeToUtf8Bytes(paymentProcessed);

            await PublishAsync(channel, _options.PaymentProcessedCatalogQueue, payload);
            await PublishAsync(channel, _options.PaymentProcessedNotificationsQueue, payload);

            await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing OrderPlacedEvent, requeuing message.");
            await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private static async Task PublishAsync(IChannel channel, string queueName, byte[] payload)
    {
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName,
            mandatory: false, basicProperties: properties, body: payload);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
            await _channel.CloseAsync(cancellationToken);
        if (_connection is not null)
            await _connection.CloseAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }
}
