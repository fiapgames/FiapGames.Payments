using FiapGames.Contracts.IntegrationEvents;
using FiapGames.Payments.Data;
using FiapGames.Payments.Models;

namespace FiapGames.Payments.Services;

public class PaymentService(PaymentsDbContext dbContext) : IPaymentService
{
    public async Task<PaymentProcessedEvent> ProcessAsync(OrderPlacedEvent orderPlaced, CancellationToken cancellationToken = default)
    {
        var status = Random.Shared.Next(2) == 0 ? PaymentStatus.Approved : PaymentStatus.Rejected;

        var paymentProcessed = new PaymentProcessedEvent(
            orderPlaced.OrderId,
            orderPlaced.UserId,
            orderPlaced.GameId,
            status,
            DateTime.UtcNow);

        dbContext.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = paymentProcessed.OrderId,
            UserId = paymentProcessed.UserId,
            GameId = paymentProcessed.GameId,
            Status = paymentProcessed.Status,
            ProcessedAt = paymentProcessed.ProcessedAt
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return paymentProcessed;
    }
}
