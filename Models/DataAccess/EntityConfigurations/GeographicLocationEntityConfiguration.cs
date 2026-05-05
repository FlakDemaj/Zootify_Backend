using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Models.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Models.DataAccess.Entitys;

namespace Zootify_Backend.Models.DataAccess.EntityConfigurations;

public class GeographicLocationEntityConfiguration : BaseGuidEntityConfiguration<GeographicLocationEntity>
{
    private string Table { get; }

    public GeographicLocationEntityConfiguration()
    {
        Table = "geo_locations";
        Schema = "public";
    }

    public override void Configure(EntityTypeBuilder<GeographicLocationEntity> builder)
    {
        builder.ToTable(Table, Schema);
        base.Configure(builder);

        builder
            .Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder
            .Property(p => p.Level)
            .HasColumnName("level")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder
            .Property(p => p.ParentGuid)
            .HasColumnName("parent_guid");

        builder
            .HasOne(g => g.Parent)
            .WithMany(g => g.Children)
            .HasForeignKey(g => g.ParentGuid)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(g => new { g.ParentGuid, g.Name })
            .IsUnique();
    }
}
