using System.Text.Json;
using System.Text.Json.Serialization;
using FiapGames.Contracts.IntegrationEvents;

namespace FiapGames.Payments.Services;

public interface INotificationsClient
{
    Task SendPaymentProcessedAsync(PaymentProcessedEvent paymentProcessed, CancellationToken cancellationToken = default);
}

/// <summary>
/// Envia o evento de pagamento processado para a Azure Function de notificações,
/// que virou serverless e não consome mais a fila. A publicação no RabbitMQ
/// continua existindo porque o Catalog ainda consome esse mesmo evento.
/// </summary>
public class NotificationsClient(HttpClient httpClient, ILogger<NotificationsClient> logger)
    : INotificationsClient
{
    // PaymentStatus precisa trafegar como string ("Approved"), não como número:
    // é assim que a Function desserializa o corpo.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task SendPaymentProcessedAsync(PaymentProcessedEvent paymentProcessed, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "api/notifications/payment-processed", paymentProcessed, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Notificação de pagamento rejeitada pela Function ({StatusCode}) para o pedido {OrderId}",
                    (int)response.StatusCode, paymentProcessed.OrderId);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            // A notificação é acessória: falha aqui não pode reprocessar o pagamento.
            logger.LogWarning(exception,
                "Falha ao notificar a Function sobre o pagamento do pedido {OrderId}", paymentProcessed.OrderId);
        }
    }
}
