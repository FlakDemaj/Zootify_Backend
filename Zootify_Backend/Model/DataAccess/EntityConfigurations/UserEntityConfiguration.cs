using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

/// <summary>
/// EF Core configuration mapping <see cref="UserEntity"/> to the <c>zootify.users</c> table.
/// </summary>
public class UserEntityConfiguration : BaseDeletionEntityConfiguration<UserEntity>
{
    /// <summary>The database table name the entity is mapped to.</summary>
    private string Table { get; }

    /// <summary>
    /// Initializes the configuration with the users table and zootify schema.
    /// </summary>
    public UserEntityConfiguration()
    {
        Table = "users";
        Schema = "zootify";
    }

    /// <summary>
    /// Configures the table mapping, columns, the ignored self-audit navigation, and the unique email index.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public override void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable(Table, Schema);
        base.Configure(builder);

        builder
            .Property(p => p.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder
            .Property(p => p.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder
            .Property(p => p.Email)
            .HasColumnName("email")
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(p => p.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512)
            .IsRequired();

        builder
            .Property(p => p.Role)
            .HasColumnName("role")
            .IsRequired();
        
        builder
            .Property(p => p.LoginAttempts)
            .HasColumnName("login_attempts")
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(p => p.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Ignore(u => u.CreatedByUser);
        builder.Ignore(u => u.CreatedByUserGuid);

        builder
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}