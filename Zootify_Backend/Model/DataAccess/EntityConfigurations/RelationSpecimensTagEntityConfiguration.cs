using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Models.DataAccess.EntityConfigurations.Base;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

public class RelationSpecimensTagEntityConfiguration : BaseGuidEntityConfiguration<RelationSpecimensTagEntity>
{
    public string Table { get; set; }

    public RelationSpecimensTagEntityConfiguration()
    {
        Schema = "zootify";
        Table = "specimens_tags";
    }

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