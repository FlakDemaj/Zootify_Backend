using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Models.DataAccess.Entitys.Base;

namespace Zootify_Backend.Models.DataAccess.EntityConfigurations.Base;

public class BaseCreationEntityConfiguration<T> : BaseGuidEntityConfiguration<T>
    where T : BaseCreationEntity
{
    public override void Configure(EntityTypeBuilder<T> builder)
    {
        base.Configure(builder);

        builder
            .Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder
            .Property(p => p.CreatedByUserGuid)
            .HasColumnName("created_by_user_guid");

        builder
            .HasOne(p => p.CreatedByUser)
            .WithMany()
            .HasForeignKey(p => p.CreatedByUserGuid)
            .OnDelete(DeleteBehavior.NoAction);
    }
}