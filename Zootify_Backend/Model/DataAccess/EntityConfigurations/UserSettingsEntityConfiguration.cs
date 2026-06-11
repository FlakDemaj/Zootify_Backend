using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

public class UserSettingsEntityConfiguration : BaseGuidEntityConfiguration<UserSettingsEntity>
{
    private string Table { get; }

    public UserSettingsEntityConfiguration()
    {
        Table = "user_settings";
        Schema = "zootify";
    }

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
