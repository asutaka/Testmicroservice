using EventBus;

namespace UserService.Contracts.Events;

public sealed record UserCreatedIntegrationEvent(
    Guid UserId,
    string Email,
    string FullName
) : IntegrationEvent;
