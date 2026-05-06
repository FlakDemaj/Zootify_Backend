using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Models.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Models.DataAccess.Entitys;

namespace Zootify_Backend.Models.DataAccess.EntityConfigurations;

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
            .Property(p => p.SpeciesGuid)
            .HasColumnName("species_guid")
            .IsRequired();

        builder
            .Property(p => p.GeographicLocationGuid)
            .HasColumnName("geo_location_guid");

        builder
            .Property(p => p.Latitude)
            .HasColumnName("latitude");

        builder
            .Property(p => p.Longitude)
            .HasColumnName("longitude");

        builder
            .Property(p => p.FindDate)
            .HasColumnName("find_date");

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
            .HasOne(s => s.Species)
            .WithMany(t => t.Specimens)
            .HasForeignKey(s => s.SpeciesGuid)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(s => s.GeographicLocation)
            .WithMany(g => g.Specimens)
            .HasForeignKey(s => s.GeographicLocationGuid)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => s.IsPublic);
        builder.HasIndex(s => s.SpeciesGuid);
    }
}
