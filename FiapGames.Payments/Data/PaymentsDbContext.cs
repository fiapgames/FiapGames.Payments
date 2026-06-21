using FiapGames.Payments.Models;
using Microsoft.EntityFrameworkCore;

namespace FiapGames.Payments.Data;

public class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.OrderId).IsUnique();
            entity.Property(p => p.Status).HasConversion<string>();
        });
    }
}
