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
    private int _loginAttempts;
    private Guid? _updatedByUserGuid;
    private DateTime? _updatedDate;
    private Guid? _deletedByUserGuid;
    private DateTime? _deletionDateTime;

    /// <summary>Sets the user's email address.</summary>
    public UserEntityBuilder WithEmail(string email) { _email = email; return this; }

    /// <summary>Sets the user's first name.</summary>
    public UserEntityBuilder WithFirstName(string name) { _firstName = name; return this; }

    /// <summary>Sets the user's last name.</summary>
    public UserEntityBuilder WithLastName(string name) { _lastName = name; return this; }

    /// <summary>Sets the user's status.</summary>
    public UserEntityBuilder WithStatus(UserStatus status) { _status = status; return this; }
    
    /// <summary>Sets the user's role.</summary>
    public UserEntityBuilder WithRole(UserRole role) { _role = role; return this; }
    
    /// <summary>Sets the user's updater.</summary>
    public UserEntityBuilder WithUpdater(Guid? updater) { _updatedByUserGuid = updater; return this; }
    
    /// <summary>Sets the user's update time.</summary>
    public UserEntityBuilder WithUpdateDateTime(DateTime? updateTime) {  _updatedDate = updateTime; return this; }
    
    /// <summary>Sets the user's deleter.</summary>
    public UserEntityBuilder WithDeleter(Guid? deleter) { _deletedByUserGuid = deleter; return this; }
    
    /// <summary>Sets the user's deletion time.</summary>
    public UserEntityBuilder WithDeletionDateTime(DateTime? deletionDate) {  _deletionDateTime = deletionDate; return this; }
    
    /// <summary>Sets the number of failed login attempts.</summary>
    public UserEntityBuilder WithLoginAttempts(int count) { _loginAttempts = count; return this; }

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
        
        Set(user, nameof(UserEntity.UpdatedByUserGuid), _updatedByUserGuid);
        Set(user, nameof(UserEntity.UpdatedDate), _updatedDate);

        Set(user, nameof(UserEntity.DeletedByUserGuid), _deletedByUserGuid);
        Set(user, nameof(UserEntity.DeletionDate), _deletionDateTime);
        
        return user;
    }

    /// <summary>
    /// Sets a named property on the given object using reflection, including private setters
    /// declared on base classes.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="BindingFlags.DeclaredOnly"/> and walks up the inheritance chain to find
    /// the exact class that declares the property. This is necessary because a <c>private set</c>
    /// defined on a base class is not accessible via reflection when starting from a derived type.
    /// </remarks>
    /// <param name="obj">The object whose property should be set.</param>
    /// <param name="propertyName">The name of the property to set.</param>
    /// <param name="value">The value to assign to the property.</param>
    /// <typeparam name="T">The type of the object.</typeparam>
    private static void Set<T>(T obj, string propertyName, object? value) where T : class
    {
        var currentType = obj.GetType();

        while (currentType != null)
        {
            var property = currentType.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (property != null)
            {
                property.SetValue(obj, value);
                return;
            }

            currentType = currentType.BaseType;
        }
    }
}
