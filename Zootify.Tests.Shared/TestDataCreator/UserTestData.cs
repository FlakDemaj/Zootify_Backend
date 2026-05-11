using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys;

using Zootify.Tests.Shared.Builders;

namespace Zootify.Tests.Shared.TestDataCreator;

public static class UserTestData
{
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
