namespace NotificationsAPI.Infrastructure.Configuration;

public static class QueueSettingsFactory
{
    public static QueueSettings FromEnvironment()
    {
        return new QueueSettings
        {
            UserCreatedQueue =
                Environment.GetEnvironmentVariable("QUEUE_USER_CREATED")
                ?? "user-created-queue",

            PaymentProcessedQueue =
                Environment.GetEnvironmentVariable("QUEUE_PAYMENT_PROCESSED")
                ?? "payment-processed-queue"
        };
    }
}