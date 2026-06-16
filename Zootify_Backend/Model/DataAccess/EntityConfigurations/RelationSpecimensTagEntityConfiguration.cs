using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

/// <summary>
/// EF Core configuration mapping the specimen↔tag join entity <see cref="RelationSpecimensTagEntity"/>
/// to the <c>zootify.specimens_tags</c> table.
/// </summary>
public class RelationSpecimensTagEntityConfiguration : BaseGuidEntityConfiguration<RelationSpecimensTagEntity>
{
    /// <summary>The database table name the entity is mapped to.</summary>
    public string Table { get; set; }

    /// <summary>
    /// Initializes the configuration with the specimens-tags join table and zootify schema.
    /// </summary>
    public RelationSpecimensTagEntityConfiguration()
    {
        Schema = "zootify";
        Table = "specimens_tags";
    }

    /// <summary>
    /// Configures the table mapping, the specimen and tag foreign keys, and the unique composite index.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public override void Configure(EntityTypeBuilder<RelationSpecimensTagEntity> builder)
    {
        builder.ToTable(Table, Schema);
        
        base.Configure(builder);

        builder
            .Property(r => r.SpecimenGuid)
            .HasColumnName("specimen_guid")
            .IsRequired();
        
        builder
            .Property(r => r.TagGuid)
            .HasColumnName("tag_guid")
            .IsRequired();

        builder.HasOne(r => r.Specimen)
            .WithMany(s => s.SpecimensTags)
            .HasForeignKey(r => r.SpecimenGuid);

        builder.HasOne(r => r.Tag)
            .WithMany(t => t.SpecimensTags)
            .HasForeignKey(r => r.TagGuid);
        
        builder.HasIndex(x => new { x.SpecimenGuid, x.TagGuid })
            .IsUnique();
    }
}