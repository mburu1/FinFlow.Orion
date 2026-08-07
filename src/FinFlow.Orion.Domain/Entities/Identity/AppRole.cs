using FinFlow.Orion.Domain.Primitives;

namespace FinFlow.Orion.Domain.Entities.Identity;

public sealed class AppRole : Entity
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsSystemRole { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<AppUser> _users = [];
    public IReadOnlyCollection<AppUser> Users => _users.AsReadOnly();

    private AppRole() { } // EF Core

    public static AppRole Create(string name, string? description = null, bool isSystemRole = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be empty.", nameof(name));

        return new AppRole
        {
            Id = Guid.NewGuid(),
            Name = name.ToUpperInvariant(),
            Description = description,
            IsSystemRole = isSystemRole,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDescription(string? description)
    {
        if (IsSystemRole)
            throw new InvalidOperationException("Cannot modify system role.");

        Description = description;
    }

    public void AssignToUser(AppUser user)
    {
        if (!_users.Contains(user))
            _users.Add(user);
    }

    public void RemoveFromUser(AppUser user)
    {
        _users.Remove(user);
    }
}