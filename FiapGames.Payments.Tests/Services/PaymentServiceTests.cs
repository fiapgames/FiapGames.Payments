using FiapGames.Contracts.IntegrationEvents;
using FiapGames.Payments.Data;
using FiapGames.Payments.Services;
using Microsoft.EntityFrameworkCore;

namespace FiapGames.Payments.Tests.Services;

public class PaymentServiceTests
{
    private static PaymentsDbContext NewContext() =>
        new(new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static OrderPlacedEvent NewOrder() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 39.90m, DateTime.UtcNow);

    [Fact]
    public async Task ProcessAsync_KeepsOrderUserAndGame()
    {
        await using var context = NewContext();
        var order = NewOrder();

        var result = await new PaymentService(context).ProcessAsync(order);

        Assert.Equal(order.OrderId, result.OrderId);
        Assert.Equal(order.UserId, result.UserId);
        Assert.Equal(order.GameId, result.GameId);
        Assert.True(result.Status is PaymentStatus.Approved or PaymentStatus.Rejected);
    }

    [Fact]
    public async Task ProcessAsync_PersistsThePaymentWithTheSameOutcome()
    {
        await using var context = NewContext();
        var order = NewOrder();

        var result = await new PaymentService(context).ProcessAsync(order);

        var saved = await context.Payments.SingleAsync();
        Assert.Equal(order.OrderId, saved.OrderId);
        Assert.Equal(result.Status, saved.Status);
        Assert.Equal(result.ProcessedAt, saved.ProcessedAt);
    }
}
