using Zootify.Tests.Shared.Builders;

namespace Zootify.Tests.Integration.Common.Helpers;

public class UserTestRepository
{
    private readonly ZootifyDbContext _dbContext;

    public UserTestRepository(ZootifyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserEntity> CreateUserAsync(
        string email,
        string password = "TestPass123!",
        UserStatus status = UserStatus.Active,
        string firstName = "Test",
        string lastName = "User",
        bool withResetToken = false)
    {
        var user = new UserEntityBuilder()
            .WithEmail(email.ToLower())
            .WithFirstName(firstName)
            .WithLastName(lastName)
            .WithStatus(status)
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
