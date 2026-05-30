using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

public class UserEntityConfiguration : BaseDeletionEntityConfiguration<UserEntity>
{
    private string Table { get; }

    public UserEntityConfiguration()
    {
        Table = "users";
        Schema = "zootify";
    }

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