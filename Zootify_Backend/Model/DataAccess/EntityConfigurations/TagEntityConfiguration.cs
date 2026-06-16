using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

/// <summary>
/// EF Core configuration mapping <see cref="TagEntity"/> to the <c>zootify.tags</c> table.
/// </summary>
public class TagEntityConfiguration : BaseGuidEntityConfiguration<TagEntity>
{
    /// <summary>The database table name the entity is mapped to.</summary>
    private string Table { get; }

    /// <summary>
    /// Initializes the configuration with the tags table and zootify schema.
    /// </summary>
    public TagEntityConfiguration()
    {
        Table = "tags";
        Schema = "zootify";
    }

    /// <summary>
    /// Configures the table mapping and the tag name column.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public override void Configure(EntityTypeBuilder<TagEntity> builder)
    {
        builder.ToTable(Table, Schema);
        
        base.Configure(builder);
        
        builder
            .Property(p => p.Tag)
            .HasColumnName("tag")
            .HasColumnType("varchar")
            .HasMaxLength(100)
            .IsRequired();
    }
}