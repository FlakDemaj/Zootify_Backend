using System.Reflection;

using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify.Tests.Shared.Builders;

public class UserEntityBuilder
{
    private string _email = $"test_{Guid.NewGuid():N}@example.com";
    private string _firstName = "Test";
    private string _lastName = "User";
    private UserRole _role = UserRole.User;
    private UserStatus _status = UserStatus.Pending;
    private int _loginAttempts = 0;
    private string? _passwordHash = null;

    public UserEntityBuilder WithEmail(string email) { _email = email; return this; }
    public UserEntityBuilder WithFirstName(string name) { _firstName = name; return this; }
    public UserEntityBuilder WithLastName(string name) { _lastName = name; return this; }
    public UserEntityBuilder WithRole(UserRole role) { _role = role; return this; }
    public UserEntityBuilder WithStatus(UserStatus status) { _status = status; return this; }
    public UserEntityBuilder WithLoginAttempts(int count) { _loginAttempts = count; return this; }
    public UserEntityBuilder WithPasswordHash(string hash) { _passwordHash = hash; return this; }
    public UserEntityBuilder Active() { _status = UserStatus.Active; return this; }
    public UserEntityBuilder Blocked() { _status = UserStatus.Blocked; return this; }

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

    private static void Set<T>(T obj, string propertyName, object? value) where T : class
    {
        var prop = obj.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        prop?.SetValue(obj, value);
    }
}
