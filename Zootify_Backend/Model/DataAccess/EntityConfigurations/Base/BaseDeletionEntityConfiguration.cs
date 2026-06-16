using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;

/// <summary>
/// Base EF Core configuration for soft-deletable entities; maps the deletion date and the (no-action)
/// foreign key to the deleting user, on top of the creation and update mappings.
/// </summary>
/// <typeparam name="T">The entity type, which must derive from <see cref="BaseDeletionEntity"/>.</typeparam>
public class BaseDeletionEntityConfiguration<T> : BaseUpdateEntityConfiguration<T>
where T : BaseDeletionEntity
{
    /// <summary>
    /// Configures the creation/update mappings plus the deletion-date and deleted-by-user mappings.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
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
            .OnDelete(DeleteBehavior.NoAction);
    }
}