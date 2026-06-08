using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Models.DataAccess.EntityConfigurations.Base;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

public class SpecimenCollectionEntityConfiguration : BaseGuidEntityConfiguration<SpecimenCollectionEntity>
{
    private string Table { get; }

    public SpecimenCollectionEntityConfiguration()
    {
        Table = "specimen_collection";
        Schema = "zootify";
    }

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