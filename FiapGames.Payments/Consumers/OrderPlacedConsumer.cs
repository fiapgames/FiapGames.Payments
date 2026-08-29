using FiapGames.Contracts.IntegrationEvents;
using FiapGames.Payments.Services;
using MassTransit;

namespace FiapGames.Payments.Consumers;

public class OrderPlacedConsumer(
    IPaymentService paymentService,
    IPublishEndpoint publishEndpoint,
    INotificationsClient notificationsClient,
    ILogger<OrderPlacedConsumer> logger)
    : IConsumer<OrderPlacedEvent>
{
    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var orderPlaced = context.Message;

        var paymentProcessed = await paymentService.ProcessAsync(orderPlaced, context.CancellationToken);

        logger.LogInformation("Payment for order {OrderId} processed with status {Status}",
            paymentProcessed.OrderId, paymentProcessed.Status);

        // Continua publicando no RabbitMQ: o Catalog consome este mesmo evento.
        await publishEndpoint.Publish(paymentProcessed, context.CancellationToken);

        // As notificações, porém, agora ficam na Azure Function serverless.
        await notificationsClient.SendPaymentProcessedAsync(paymentProcessed, context.CancellationToken);
    }
}
