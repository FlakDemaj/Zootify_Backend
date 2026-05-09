using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Models.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Models.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

public class RegistrationTokenEntityConfiguration : BaseGuidEntityConfiguration<RegistrationTokenEntity>
{
    private string Table { get; }

    public RegistrationTokenEntityConfiguration()
    {
        Table = "registration_tokens";
        Schema = "zootify";
    }
    
    public override void Configure(EntityTypeBuilder<RegistrationTokenEntity> builder)
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
            .WithOne(u => u.RegistrationToken)
            .HasForeignKey<RegistrationTokenEntity>(t => t.UserGuid)
            .OnDelete(DeleteBehavior.Cascade);
    }
}