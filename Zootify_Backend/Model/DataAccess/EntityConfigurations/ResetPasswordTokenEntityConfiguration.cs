using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

/// <summary>
/// EF Core configuration mapping <see cref="ResetPasswordTokenEntity"/> to the <c>zootify.reset_password_tokens</c> table.
/// </summary>
public class ResetPasswordTokenEntityConfiguration : BaseGuidEntityConfiguration<ResetPasswordTokenEntity>
{
    /// <summary>The database table name the entity is mapped to.</summary>
    private string Table { get; }

    /// <summary>
    /// Initializes the configuration with the reset-password-tokens table and zootify schema.
    /// </summary>
    public ResetPasswordTokenEntityConfiguration()
    {
        Table = "reset_password_tokens";
        Schema = "zootify";
    }

    /// <summary>
    /// Configures the table mapping, columns, and the one-to-one relationship to the user.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public override void Configure(EntityTypeBuilder<ResetPasswordTokenEntity> builder)
    {
        builder.ToTable(Table, Schema);
        
        base.Configure(builder);

        builder
            .Property(t => t.Token)
            .HasColumnName("token")
            .HasMaxLength(128)
            .IsRequired();

        builder
            .Property(t => t.UserGuid)
            .HasColumnName("user_guid")
            .IsRequired();

        builder
            .Property(t => t.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder
            .HasOne(t => t.User)
            .WithOne(u => u.ResetPasswordToken)
            .HasForeignKey<ResetPasswordTokenEntity>(t => t.UserGuid)
            .OnDelete(DeleteBehavior.Cascade);
    }
}