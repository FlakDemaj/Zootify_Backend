using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

/// <summary>
/// EF Core configuration mapping the specimen↔collection join entity <see cref="SpecimenCollectionEntity"/>
/// to the <c>zootify.specimen_collection</c> table.
/// </summary>
public class SpecimenCollectionEntityConfiguration : BaseGuidEntityConfiguration<SpecimenCollectionEntity>
{
    /// <summary>The database table name the entity is mapped to.</summary>
    private string Table { get; }

    /// <summary>
    /// Initializes the configuration with the specimen-collection join table and zootify schema.
    /// </summary>
    public SpecimenCollectionEntityConfiguration()
    {
        Table = "specimen_collection";
        Schema = "zootify";
    }

    /// <summary>
    /// Configures the table mapping and the cascade-deleting specimen and collection foreign keys.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public override void Configure(EntityTypeBuilder<SpecimenCollectionEntity> builder)
    {
        builder.ToTable(Table, Schema);
        
        base.Configure(builder);
        
        builder
            .Property(e => e.CollectionGuid)
            .HasColumnName("collection_guid")
            .IsRequired();

        builder
            .Property(e => e.SpecimenGuid)
            .HasColumnName("specimen_guid")
            .IsRequired();        
        
        builder
            .HasOne(e => e.Specimen)
            .WithMany(s => s.Collections)
            .HasForeignKey(e => e.SpecimenGuid)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder
            .HasOne(e => e.Collection)
            .WithMany(s => s.Collection)
            .HasForeignKey(e => e.CollectionGuid)
            .OnDelete(DeleteBehavior.Cascade);
            
        
    }
    
}