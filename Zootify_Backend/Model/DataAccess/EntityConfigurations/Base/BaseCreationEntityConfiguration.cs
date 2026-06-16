using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;

/// <summary>
/// Base EF Core configuration for entities that track creation metadata; maps the creation date and
/// the (no-action) foreign key to the creating user.
/// </summary>
/// <typeparam name="T">The entity type, which must derive from <see cref="BaseCreationEntity"/>.</typeparam>
public class BaseCreationEntityConfiguration<T> : BaseGuidEntityConfiguration<T>
    where T : BaseCreationEntity
{
    /// <summary>
    /// Configures the base key plus the creation-date and created-by-user mappings.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public override void Configure(EntityTypeBuilder<T> builder)
    {
        base.Configure(builder);

        builder
            .Property(p => p.CreationDate)
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