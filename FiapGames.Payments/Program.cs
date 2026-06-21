using FiapGames.Payments.Configuration;
using FiapGames.Payments.Data;
using FiapGames.Payments.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var rabbitMqOptions = builder.Configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
    ?? new RabbitMqOptions();

builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.AddConsumers(typeof(Program).Assembly);

    busConfigurator.UsingRabbitMq((context, rabbitMqConfigurator) =>
    {
        rabbitMqConfigurator.Host(rabbitMqOptions.Host, rabbitMqOptions.VirtualHost, host =>
        {
            host.Username(rabbitMqOptions.UserName);
            host.Password(rabbitMqOptions.Password);
        });

        rabbitMqConfigurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("payments", false));
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
