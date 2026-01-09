using MassTransit;
using NotificationsAPI.Application.UseCases;
using Shared.Contracts.Events;

namespace NotificationsAPI.Infrastructure.Messaging.Consumers;

public class PaymentProcessedConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly SendPurchaseConfirmationUseCase _useCase;

    public PaymentProcessedConsumer(SendPurchaseConfirmationUseCase useCase)
    {
        _useCase = useCase;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var message = context.Message;

        await _useCase.ExecuteAsync(
            email: "user@email.com",
            paymentStatus: message.Status
        );
    }
}