using AspNetCore.HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NotificationsAPI;
using NotificationsAPI.Application.Ports;
using NotificationsAPI.Application.UseCases;
using NotificationsAPI.Domain.Services;
using NotificationsAPI.Infrastructure.Email;
using NotificationsAPI.Infrastructure.Messaging.Consumers;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddHealthChecks()
    .AddRabbitMQ(_ =>
    {
        var factory = new RabbitMQ.Client.ConnectionFactory
        {
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
            UserName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? string.Empty,
            Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? string.Empty,
            VirtualHost = "/"
        };

        return factory.CreateConnectionAsync().GetAwaiter().GetResult();
    }, name: "rabbitmq");

builder.Services.AddScoped<INotificationDomainService, NotificationDomainService>();
builder.Services.AddScoped<SendWelcomeEmailUseCase>();
builder.Services.AddScoped<SendPurchaseConfirmationUseCase>();
builder.Services.AddScoped<IEmailSender, ConsoleEmailSender>();

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
                h.Username(Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? string.Empty);
                h.Password(Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? string.Empty);
            });

        cfg.ReceiveEndpoint("fcg.notifications.user-created", e =>
        {
            e.ConfigureConsumeTopology = false;
            e.Bind("fcg.user-created-event", s =>
            {
                s.RoutingKey = "notifications.user-created";
            });
            e.ConfigureConsumer<UserCreatedIntegrationEventConsumer>(context);
        });

        cfg.Message<Shared.Contracts.Events.PaymentProcessedEvent>(m =>
        {
            m.SetEntityName("fcg.payment-processed-event");
        });

        cfg.ReceiveEndpoint("fcg.notifications.payment-processed", e =>
        {
            e.ConfigureConsumeTopology = false;
            e.Bind("fcg.payment-processed-event");
            e.ConfigureConsumer<PaymentProcessedConsumer>(context);
        });
    });
});

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapGet("/", () => Results.Ok(new { service = "notifications-api", status = "running" }));

app.Run();
