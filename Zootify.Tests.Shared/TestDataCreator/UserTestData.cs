using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys;

using Zootify.Tests.Shared.Builders;

namespace Zootify.Tests.Shared.TestDataCreator;

/// <summary>
/// Factory for creating in-memory <see cref="UserEntity"/> instances for unit tests.
/// </summary>
public static class UserTestData
{
    /// <summary>
    /// Creates an in-memory <see cref="UserEntity"/> with the given parameters using the fluent builder.
    /// The entity is not persisted to any database.
    /// </summary>
    /// <param name="status">The user's initial status (default: Active).</param>
    /// <param name="loginAttempts">The number of pre-set failed login attempts.</param>
    /// <param name="firstName">The user's first name.</param>
    /// <param name="lastName">The user's last name.</param>
    public static UserEntity CreateUser(
        UserStatus status = UserStatus.Active,
        int loginAttempts = 0,
        string firstName = "Test",
        string lastName = "User")
    {
        return new UserEntityBuilder()
            .WithStatus(status)
            .WithLoginAttempts(loginAttempts)
            .WithFirstName(firstName)
            .WithLastName(lastName)
            .Build();
    }
}
