using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;

/// <summary>
/// Base EF Core configuration for entities that track update metadata; maps the update date and the
/// (no-action) foreign key to the updating user, on top of the creation mappings.
/// </summary>
/// <typeparam name="T">The entity type, which must derive from <see cref="BaseUpdateEntity"/>.</typeparam>
public class BaseUpdateEntityConfiguration<T> : BaseCreationEntityConfiguration<T>
where T : BaseUpdateEntity
{
    /// <summary>
    /// Configures the creation mappings plus the update-date and updated-by-user mappings.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
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