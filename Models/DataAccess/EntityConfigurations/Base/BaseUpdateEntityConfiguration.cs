using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Models.DataAccess.Entitys.Base;

namespace Zootify_Backend.Models.DataAccess.EntityConfigurations.Base;

public class BaseUpdateEntityConfiguration<T> : BaseCreationEntityConfiguration<T>
where T : BaseUpdateEntity
{
    public override void Configure(EntityTypeBuilder<T> builder)
    {
        base.Configure(builder);

        builder
            .Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder
            .Property(p => p.UpdatedBy)
            .HasColumnName("updated_by_user_guid");

        builder
            .HasOne(p => p.UpdatedByUser)
            .WithMany()
            .HasForeignKey(p => p.UpdatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}