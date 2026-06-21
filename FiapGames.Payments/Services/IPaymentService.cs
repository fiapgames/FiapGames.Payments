using FiapGames.Contracts.IntegrationEvents;

namespace FiapGames.Payments.Services;

public interface IPaymentService
{
    Task<PaymentProcessedEvent> ProcessAsync(OrderPlacedEvent orderPlaced, CancellationToken cancellationToken = default);
}
