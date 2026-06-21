using FiapGames.Payments;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));

builder.Services.AddHostedService<PaymentWorker>();

var host = builder.Build();
host.Run();
