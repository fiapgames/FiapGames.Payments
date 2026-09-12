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

        // Negócio: o Catalog consome este evento para aprovar/rejeitar o pedido e
        // liberar o jogo na biblioteca. Ao contrário do UserCreatedEvent no Users
        // — que só notificava e por isso saiu do RabbitMQ — este Publish PERMANECE.
        await publishEndpoint.Publish(paymentProcessed, context.CancellationToken);

        // Notificação: só o e-mail de confirmação de compra virou serverless.
        await notificationsClient.SendPaymentProcessedAsync(paymentProcessed, context.CancellationToken);
    }
}
