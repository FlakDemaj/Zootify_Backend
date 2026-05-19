using Zootify.Tests.Shared.Builders;

namespace Zootify.Tests.Integration.Utils.Helpers;

/// <summary>
/// Test helper for creating and persisting <see cref="UserEntity"/> instances directly
/// in the database, bypassing the application's service layer.
/// </summary>
public class UserTestRepository
{
    private readonly ZootifyDbContext _dbContext;

    /// <summary>
    /// Initializes a new <see cref="UserTestRepository"/> with the shared test database context.
    /// </summary>
    public UserTestRepository(ZootifyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Creates and persists a user with the given properties.
    /// Automatically creates a registration token for pending users and a reset-password token
    /// when <paramref name="withResetToken"/> is <c>true</c>.
    /// </summary>
    /// <param name="email">The user's email address (stored lowercase).</param>
    /// <param name="password">The plain-text password to hash and store.</param>
    /// <param name="status">The initial status of the user.</param>
    /// <param name="role">The role of the user</param>
    /// <param name="firstName">The user's first name.</param>
    /// <param name="lastName">The user's last name.</param>
    /// <param name="updateDateTime">The DateTime of the last update</param>
    /// <param name="wrongLoginAttempts">Login Attempts of the User</param>
    /// <param name="withResetToken">Whether to also persist a reset-password token for the user.</param>
    /// <param name="deleter">The Guid of the deleter of the User</param>
    /// <param name="deletionDateTime">The DateTime of the last deletion</param>
    /// <param name="updater">The Guid of the updater of the User</param>
    /// <returns>The persisted <see cref="UserEntity"/> including any created tokens.</returns>
    public async Task<UserEntity> CreateUserAsync(
        string email,
        string password = "TestPass123!",
        UserStatus status = UserStatus.Active,
        UserRole role = UserRole.User,
        string firstName = "Test",
        string lastName = "User",
        Guid? deleter = null,
        DateTime? deletionDateTime = null,
        Guid? updater = null,
        DateTime? updateDateTime = null,
        int wrongLoginAttempts = 0,
        bool withResetToken = false)
    {
        var user = new UserEntityBuilder()
            .WithEmail(email.ToLower())
            .WithFirstName(firstName)
            .WithRole(role)
            .WithLastName(lastName)
            .WithStatus(status)
            .WithUpdater(updater)
            .WithUpdateDateTime(updateDateTime)
            .WithDeleter(deleter)
            .WithLoginAttempts(wrongLoginAttempts)
            .WithDeletionDateTime(deletionDateTime)
            .Build();

        user.SetPassword(password);

        if (status == UserStatus.Pending)
            user.CreateRegistrationToken(24);

        _dbContext.Add(user);
        await _dbContext.SaveChangesAsync();

        if (withResetToken)
        {
            var token = user.CreateResetPasswordTokenEntityToken(24);
            _dbContext.Add(token);
            await _dbContext.SaveChangesAsync();
        }

        return user;
    }
}
