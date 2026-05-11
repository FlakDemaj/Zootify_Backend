using System.Reflection;

using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify.Tests.Shared.Builders;

/// <summary>
/// Fluent builder for constructing <see cref="UserEntity"/> instances in tests
/// without going through the application's service layer.
/// Uses reflection to set private properties that have no public setters.
/// </summary>
public class UserEntityBuilder
{
    private string _email = $"test_{Guid.NewGuid():N}@example.com";
    private string _firstName = "Test";
    private string _lastName = "User";
    private UserRole _role = UserRole.User;
    private UserStatus _status = UserStatus.Pending;
    private int _loginAttempts = 0;
    private string? _passwordHash = null;

    /// <summary>Sets the user's email address.</summary>
    public UserEntityBuilder WithEmail(string email) { _email = email; return this; }

    /// <summary>Sets the user's first name.</summary>
    public UserEntityBuilder WithFirstName(string name) { _firstName = name; return this; }

    /// <summary>Sets the user's last name.</summary>
    public UserEntityBuilder WithLastName(string name) { _lastName = name; return this; }

    /// <summary>Sets the user's role.</summary>
    public UserEntityBuilder WithRole(UserRole role) { _role = role; return this; }

    /// <summary>Sets the user's status.</summary>
    public UserEntityBuilder WithStatus(UserStatus status) { _status = status; return this; }

    /// <summary>Sets the number of failed login attempts.</summary>
    public UserEntityBuilder WithLoginAttempts(int count) { _loginAttempts = count; return this; }

    /// <summary>Sets the pre-hashed password directly, bypassing BCrypt hashing.</summary>
    public UserEntityBuilder WithPasswordHash(string hash) { _passwordHash = hash; return this; }

    /// <summary>Shorthand for setting the status to <see cref="UserStatus.Active"/>.</summary>
    public UserEntityBuilder Active() { _status = UserStatus.Active; return this; }

    /// <summary>Shorthand for setting the status to <see cref="UserStatus.Blocked"/>.</summary>
    public UserEntityBuilder Blocked() { _status = UserStatus.Blocked; return this; }

    /// <summary>
    /// Constructs the <see cref="UserEntity"/> by injecting all configured values
    /// via reflection into the private properties.
    /// </summary>
    public UserEntity Build()
    {
        var user = new UserEntity();
        Set(user, nameof(UserEntity.Email), _email);
        Set(user, nameof(UserEntity.FirstName), _firstName);
        Set(user, nameof(UserEntity.LastName), _lastName);
        Set(user, nameof(UserEntity.Role), _role);
        Set(user, nameof(UserEntity.Status), _status);
        Set(user, nameof(UserEntity.LoginAttempts), _loginAttempts);

        if (_passwordHash is not null)
            Set(user, nameof(UserEntity.PasswordHash), _passwordHash);

        return user;
    }

    /// <summary>
    /// Sets a named property on the given object using reflection, including private setters.
    /// </summary>
    private static void Set<T>(T obj, string propertyName, object? value) where T : class
    {
        var prop = obj.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        prop?.SetValue(obj, value);
    }
}
