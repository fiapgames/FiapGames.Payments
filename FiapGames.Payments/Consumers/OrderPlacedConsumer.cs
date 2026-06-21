using FiapGames.Contracts.IntegrationEvents;
using FiapGames.Payments.Services;
using MassTransit;

namespace FiapGames.Payments.Consumers;

public class OrderPlacedConsumer(IPaymentService paymentService, IPublishEndpoint publishEndpoint, ILogger<OrderPlacedConsumer> logger)
    : IConsumer<OrderPlacedEvent>
{
    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var orderPlaced = context.Message;

        var paymentProcessed = await paymentService.ProcessAsync(orderPlaced, context.CancellationToken);

        logger.LogInformation("Payment for order {OrderId} processed with status {Status}",
            paymentProcessed.OrderId, paymentProcessed.Status);

        await publishEndpoint.Publish(paymentProcessed, context.CancellationToken);
    }
}
