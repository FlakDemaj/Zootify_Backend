using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.Entitys.Base;
using Zootify_Backend.Models.DataAccess.Entitys.Base;

namespace Zootify_Backend.Models.DataAccess.EntityConfigurations.Base;

public class BaseUpdateEntityConfiguration<T> : BaseCreationEntityConfiguration<T>
where T : BaseUpdateEntity
{
    public override void Configure(EntityTypeBuilder<T> builder)
    {
        base.Configure(builder);

        builder
            .Property(p => p.UpdatedDate)
            .HasColumnName("updated_date");

        builder
            .Property(p => p.UpdatedByUserGuid)
            .HasColumnName("updated_by_user_guid");

        builder
            .HasOne(p => p.UpdatedByUser)
            .WithMany()
            .HasForeignKey(p => p.UpdatedByUserGuid)
            .OnDelete(DeleteBehavior.NoAction);
    }
}