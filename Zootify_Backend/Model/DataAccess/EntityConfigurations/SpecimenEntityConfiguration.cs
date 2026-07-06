using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

/// <summary>
/// EF Core configuration mapping <see cref="SpecimenEntity"/> to the <c>zootify.specimens</c> table,
/// including its species, location, and audit-user relationships.
/// </summary>
public class SpecimenEntityConfiguration : BaseDeletionEntityConfiguration<SpecimenEntity>
{
    /// <summary>The database table name the entity is mapped to.</summary>
    private string Table { get; }

    /// <summary>
    /// Initializes the configuration with the specimens table and zootify schema.
    /// </summary>
    public SpecimenEntityConfiguration()
    {
        Table = "specimens";
        Schema = "zootify";
    }

    /// <summary>
    /// Configures the table mapping, columns, the species/location/audit relationships, and the indexes.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public override void Configure(EntityTypeBuilder<SpecimenEntity> builder)
    {
        builder.ToTable(Table, Schema);
        base.Configure(builder);

        builder
            .Property(p => p.TaxonomySpeciesGuid)
            .HasColumnName("species_guid");

        builder
            .Property(p => p.RejectedAt)
            .HasColumnName("rejected_at");
        
        builder
            .Property(p => p.Name)
            .HasColumnName("name")
            .IsRequired();
        
        builder
            .Property(p => p.LatinName)
            .HasColumnName("latin_name")
            .IsRequired();
        
        builder
            .Property(p => p.DateOfDiscovery)
            .HasColumnName("date_of_discovery")
            .IsRequired();

        builder
            .Property(p => p.GeographicLocationGuid)
            .HasColumnName("geo_location_guid")
            .IsRequired();

        builder
            .Property(p => p.Latitude)
            .HasColumnName("latitude")
            .IsRequired();

        builder
            .Property(p => p.Longitude)
            .HasColumnName("longitude")
            .IsRequired();

        builder
            .Property(p => p.StorageBuilding)
            .HasColumnName("storage_building")
            .HasMaxLength(100);

        builder
            .Property(p => p.StorageCabinet)
            .HasColumnName("storage_cabinet")
            .HasMaxLength(50);

        builder
            .Property(p => p.StorageShelf)
            .HasColumnName("storage_shelf")
            .HasMaxLength(50);

        builder
            .Property(p => p.StorageCompartment)
            .HasColumnName("storage_compartment")
            .HasMaxLength(50);

        builder
            .Property(p => p.Comment)
            .HasColumnName("comment")
            .HasMaxLength(2000);

        builder
            .Property(p => p.Size)
            .HasColumnName("size");

        builder
            .Property(p => p.Weight)
            .HasColumnName("weight");

        builder
            .Property(p => p.IsPublic)
            .HasColumnName("is_public");

        builder
            .Property(p => p.IsPublic)
            .HasColumnName("is_public");
        
        builder
            .HasOne(s => s.Species)
            .WithMany(t => t.Specimens)
            .HasForeignKey(s => s.TaxonomySpeciesGuid)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(s => s.GeographicLocation)
            .WithMany(g => g.Specimens)
            .HasForeignKey(s => s.GeographicLocationGuid)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder
            .HasOne(t => t.CreatedByUser)
            .WithMany(u => u.CreatedSpecimens)
            .HasForeignKey(t => t.CreatedByUserGuid)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder
            .HasOne(t => t.UpdatedByUser)
            .WithMany(u => u.UpdatedSpecimens)
            .HasForeignKey(t => t.UpdatedByUserGuid)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder
            .HasOne(t => t.DeletedByUser)
            .WithMany(u => u.DeletedSpecimens)
            .HasForeignKey(t => t.DeletedByUserGuid)
            .OnDelete(DeleteBehavior.NoAction);


        builder.HasIndex(s => s.IsPublic);
        builder.HasIndex(s => s.TaxonomySpeciesGuid);
    }
}
