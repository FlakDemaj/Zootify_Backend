using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;

/// <summary>
/// Base EF Core configuration for entities with a <c>guid</c> primary key; maps the key column and
/// is extended by the audit-aware base configurations.
/// </summary>
/// <typeparam name="T">The entity type, which must derive from <see cref="BaseGuidEntity"/>.</typeparam>
public class BaseGuidEntityConfiguration<T> : IEntityTypeConfiguration<T>
where T: BaseGuidEntity
{
    /// <summary>The database schema the entity is mapped to (defaults to "public").</summary>
    protected string Schema;

    /// <summary>
    /// Initializes the configuration with the default "public" schema.
    /// </summary>
    public BaseGuidEntityConfiguration()
    {
        Schema = "public";
    }

    /// <summary>
    /// Configures the <c>guid</c> primary key column for the entity.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder
            .Property(p => p.Guid)
            .IsRequired()
            .HasColumnName("guid")
            .ValueGeneratedNever();

        builder.HasKey(p => p.Guid);
    }
}