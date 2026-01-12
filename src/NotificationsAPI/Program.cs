using MassTransit;
using NotificationsAPI;
using NotificationsAPI.Application.Ports;
using NotificationsAPI.Application.UseCases;
using NotificationsAPI.Domain.Services;
using NotificationsAPI.Infrastructure.Configuration;
using NotificationsAPI.Infrastructure.Email;
using NotificationsAPI.Infrastructure.Messaging.Consumers;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();

builder.Services.AddSerilog((services, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: "Logs/notifications-worker-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            shared: true
        );
});

builder.Services.AddHostedService<Worker>();

builder.Services.AddScoped<INotificationDomainService, NotificationDomainService>();

builder.Services.AddScoped<SendWelcomeEmailUseCase>();
builder.Services.AddScoped<SendPurchaseConfirmationUseCase>();

builder.Services.AddScoped<IEmailSender, ConsoleEmailSender>();

var queueSettings = QueueSettingsFactory.FromEnvironment();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserCreatedIntegrationEventConsumer>();
    x.AddConsumer<PaymentProcessedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(
            Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
            "/",
            h =>
            {
                h.Username(
                    Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "guest");
                h.Password(
                    Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest");
            });

        // Bind to existing exchange created by UsersAPI (producer)
        // Do not specify ExchangeType to avoid trying to create it
        cfg.ReceiveEndpoint("fcg.notifications.user-created", e =>
        {
            e.ConfigureConsumeTopology = false;
            e.Bind("fcg.user-created-event", s =>
            {
                // Bind to existing exchange with routing key
                s.RoutingKey = "notifications.user-created";
            });
            e.ConfigureConsumer<UserCreatedIntegrationEventConsumer>(context);
        });

        // Bind to existing exchange/queue created by PaymentsAPI (producer)
        // Do not specify ExchangeType to avoid trying to create it
        cfg.ReceiveEndpoint("fcg.notifications.payment-processed", e =>
        {
            e.ConfigureConsumeTopology = false;
            e.Bind("fcg.payment-processed-event", s =>
            {
                // Bind to existing exchange with routing key
                s.RoutingKey = "notifications.payment-processed";
            });
            e.ConfigureConsumer<PaymentProcessedConsumer>(context);
        });
    });
});

var host = builder.Build();
host.Run();
