using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

public class SpecimenEntityConfiguration : BaseDeletionEntityConfiguration<SpecimenEntity>
{
    private string Table { get; }

    public SpecimenEntityConfiguration()
    {
        Table = "specimens";
        Schema = "zootify";
    }

    public override void Configure(EntityTypeBuilder<SpecimenEntity> builder)
    {
        builder.ToTable(Table, Schema);
        base.Configure(builder);

        builder
            .Property(p => p.TaxonomySpeciesGuid)
            .HasColumnName("species_guid")
            .IsRequired();
        
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
            .Property(p => p.IsPublic)
            .HasColumnName("is_public");

        builder
            .Property(p => p.IsPublic)
            .HasColumnName("staus");
        
        builder
            .HasOne(s => s.Species)
            .WithMany(t => t.Specimens)
            .HasForeignKey(s => s.TaxonomySpeciesGuid)
            .OnDelete(DeleteBehavior.Restrict);

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
