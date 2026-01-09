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

        cfg.ReceiveEndpoint(queueSettings.UserCreatedQueue, e =>
        {
            e.ConfigureConsumer<UserCreatedIntegrationEventConsumer>(context);
        });

        cfg.ReceiveEndpoint(queueSettings.PaymentProcessedQueue, e =>
        {
            e.ConfigureConsumer<PaymentProcessedConsumer>(context);
        });
    });
});

var host = builder.Build();
host.Run();
