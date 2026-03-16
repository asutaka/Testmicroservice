using SharedKernel.Events;
using UserService.Domain.ValueObjects;

namespace UserService.Domain.DomainEvents;

public sealed class UserCreatedDomainEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string FullName { get; }

    public UserCreatedDomainEvent(Guid userId, string email, string fullName)
    {
        UserId = userId;
        Email = email;
        FullName = fullName;
    }
}
