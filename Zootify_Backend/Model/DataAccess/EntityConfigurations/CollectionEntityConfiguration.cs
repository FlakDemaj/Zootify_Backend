using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

/// <summary>
/// EF Core configuration mapping <see cref="CollectionEntity"/> to the <c>zootify.collections</c> table.
/// </summary>
public class CollectionEntityConfiguration : BaseDeletionEntityConfiguration<CollectionEntity>
{
    /// <summary>The database table name the entity is mapped to.</summary>
    private string Table { get; }

    /// <summary>
    /// Initializes the configuration with the collections table and zootify schema.
    /// </summary>
    public CollectionEntityConfiguration()
    {
        Table = "collections";
        Schema = "zootify";
    }

    /// <summary>
    /// Configures the table mapping and the collection-specific column mappings.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public override void Configure(EntityTypeBuilder<CollectionEntity> builder)
    {
        builder.ToTable(Table, Schema);
        
        base.Configure(builder);
        
        builder
            .Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder
            .Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(400);
        
        builder
            .Property(e => e.IsPublic)
            .HasColumnName("is_public")
            .IsRequired();

        builder
            .Property(e => e.Status)
            .HasColumnName("status")
            .IsRequired();
    }

}