using SharedKernel.Entities;
using UserService.Domain.DomainEvents;
using UserService.Domain.ValueObjects;

namespace UserService.Domain.Entities;

public sealed class User : BaseEntity<Guid>
{
    public Email Email { get; private set; } = null!;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public UserRole Role { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    // For EF Core
    private User() { }

    private User(Guid id, Email email, string firstName, string lastName, string passwordHash, UserRole role)
    {
        Id = id;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;

        AddDomainEvent(new UserCreatedDomainEvent(id, email.Value, FullName));
    }

    public static User Create(
        Email email,
        string firstName,
        string lastName,
        string passwordHash,
        UserRole role = UserRole.Customer)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));

        return new User(Guid.NewGuid(), email, firstName.Trim(), lastName.Trim(), passwordHash, role);
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty.", nameof(lastName));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        SetUpdatedAt();
    }

    public void ChangeEmail(Email newEmail)
    {
        Email = newEmail;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }

    public void Activate()
    {
        IsActive = true;
        SetUpdatedAt();
    }
}

public enum UserRole
{
    Customer = 0,
    Admin = 1,
    Moderator = 2
}
