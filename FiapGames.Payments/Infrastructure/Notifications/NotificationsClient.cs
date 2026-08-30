using System.Text.Json;
using System.Text.Json.Serialization;
using FiapGames.Contracts.IntegrationEvents;

namespace FiapGames.Payments.Infrastructure.Notifications;

public interface INotificationsClient
{
    Task SendPaymentProcessedAsync(PaymentProcessedEvent paymentProcessed, CancellationToken cancellationToken = default);
}

/// <summary>
/// Envia o resultado do pagamento (aprovado/rejeitado) para a Azure Function de
/// notificações, que manda o e-mail de confirmação de compra. O RabbitMQ continua
/// carregando o mesmo evento para o Catalog (aprovação do pedido e liberação na
/// biblioteca) — só a parte de notificação virou serverless.
/// </summary>
public sealed class NotificationsClient(HttpClient httpClient, ILogger<NotificationsClient> logger)
    : INotificationsClient
{
    // PaymentStatus e demais enums trafegam como string no ecossistema FiapGames.
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
            // A notificação é acessória: falha aqui não pode impedir o processamento do pagamento.
            logger.LogWarning(exception,
                "Falha ao notificar a Function sobre o pagamento do pedido {OrderId}", paymentProcessed.OrderId);
        }
    }
}
