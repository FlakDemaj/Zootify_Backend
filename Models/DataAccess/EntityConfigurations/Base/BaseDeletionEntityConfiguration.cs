using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Models.DataAccess.Entitys.Base;

namespace Zootify_Backend.Models.DataAccess.EntityConfigurations.Base;

public class BaseDeletionEntityConfiguration<T> : BaseUpdateEntityConfiguration<T>
where T : BaseDeletionEntity
{
    public override void Configure(EntityTypeBuilder<T> builder)
    {
        base.Configure(builder);

        builder
            .Property(p => p.DeletionDate)
            .HasColumnName("deletion_date");

        builder
            .Property(p => p.DeletedByUserGuid)
            .HasColumnName("deleted_by_user_guid");

        builder
            .HasOne(p => p.DeletedByUser)
            .WithMany()
            .HasForeignKey(p => p.DeletedByUserGuid)
            .OnDelete(DeleteBehavior.SetNull);
    }
}