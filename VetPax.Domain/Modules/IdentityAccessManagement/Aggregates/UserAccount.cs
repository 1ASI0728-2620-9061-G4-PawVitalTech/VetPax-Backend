using VetPax.Domain.Modules.IdentityAccessManagement.Events;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;
using VetPax.Domain.Shared.Events;

namespace VetPax.Domain.Modules.IdentityAccessManagement.Aggregates;

public sealed class UserAccount
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public UserId UserId { get; }
    public string Email { get; }
    public UserRole Role { get; }
    public DateTime CreatedAt { get; }

    private UserAccount(
        UserId userId,
        string email,
        UserRole role)
    {
        if (userId.Value == Guid.Empty)
            throw new ArgumentException(
                "A valid user ID is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException(
                "Email is required.");

        if (!Enum.IsDefined(role))
            throw new ArgumentException(
                "Invalid user role.");

        UserId = userId;
        Email = email.Trim();
        Role = role;
        CreatedAt = DateTime.UtcNow;

        _domainEvents.Add(
            new UserAccountCreatedEvent(
                UserId,
                Role,
                CreatedAt
            )
        );
    }

    // Only call after successful identity provisioning.
    public static UserAccount CreateFromVerifiedIdentity(
        UserId userId,
        string email,
        UserRole role)
    {
        return new UserAccount(userId, email, role);
    }

    public IReadOnlyList<IDomainEvent> PullDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }
}