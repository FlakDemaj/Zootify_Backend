using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

/// <summary>
/// EF Core configuration mapping <see cref="UserSettingsEntity"/> to the <c>zootify.user_settings</c> table.
/// </summary>
public class UserSettingsEntityConfiguration : BaseGuidEntityConfiguration<UserSettingsEntity>
{
    /// <summary>The database table name the entity is mapped to.</summary>
    private string Table { get; }

    /// <summary>
    /// Initializes the configuration with the user-settings table and zootify schema.
    /// </summary>
    public UserSettingsEntityConfiguration()
    {
        Table = "user_settings";
        Schema = "zootify";
    }

    /// <summary>
    /// Configures the table mapping, columns, the one-to-one relationship to the user, and the unique index.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public override void Configure(EntityTypeBuilder<UserSettingsEntity> builder)
    {
        builder.ToTable(Table, Schema);

        base.Configure(builder);

        builder
            .Property(s => s.UserGuid)
            .HasColumnName("user_guid")
            .IsRequired();

        builder
            .Property(s => s.DarkMode)
            .HasColumnName("dark_mode")
            .IsRequired();
        
        builder
            .Property(s => s.SendPushNotification)
            .HasColumnName("send_push_notifications")
            .HasDefaultValue(true)
            .IsRequired();

        builder
            .Property(s => s.SendEmail)
            .HasColumnName("send_email_notifications")
            .HasDefaultValue(true)
            .IsRequired();

        builder
            .Property(s => s.Language)
            .HasColumnName("language")
            .HasMaxLength(10)
            .IsRequired();

        builder
            .HasOne<UserEntity>()
            .WithOne(u => u.Settings)
            .HasForeignKey<UserSettingsEntity>(s => s.UserGuid)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.UserGuid).IsUnique();
    }
}
