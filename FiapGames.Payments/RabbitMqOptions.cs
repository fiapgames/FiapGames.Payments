namespace FiapGames.Payments;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string OrderPlacedQueue { get; set; } = "order-placed-queue";
    public string PaymentProcessedCatalogQueue { get; set; } = "payment-processed-catalog-queue";
    public string PaymentProcessedNotificationsQueue { get; set; } = "payment-processed-notifications-queue";
}
