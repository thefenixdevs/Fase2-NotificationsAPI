using MassTransit;
using NotificationsAPI.Application.UseCases;
using Shared.Contracts.Events;

namespace NotificationsAPI.Infrastructure.Messaging.Consumers
{
    public class UserCreatedIntegrationEventConsumer : IConsumer<UserCreatedIntegrationEvent>
    {
        private readonly SendWelcomeEmailUseCase _useCase;

        public UserCreatedIntegrationEventConsumer(SendWelcomeEmailUseCase useCase)
        {
            _useCase = useCase;
        }

        public async Task Consume(ConsumeContext<UserCreatedIntegrationEvent> context)
        {
            var email = context.Message.Email;

            await _useCase.ExecuteAsync(email);
        }
    }
}

