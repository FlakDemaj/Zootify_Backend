using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

/// <summary>
/// EF Core configuration mapping <see cref="GeographicLocationEntity"/> to the <c>zootify.geo_locations</c> table.
/// </summary>
public class GeographicLocationEntityConfiguration : BaseGuidEntityConfiguration<GeographicLocationEntity>
{
    /// <summary>The database table name the entity is mapped to.</summary>
    private string Table { get; }

    /// <summary>
    /// Initializes the configuration with the geo-locations table and zootify schema.
    /// </summary>
    public GeographicLocationEntityConfiguration()
    {
        Table = "geo_locations";
        Schema = "zootify";
    }

    /// <summary>
    /// Configures the table mapping, columns, the self-referencing parent relationship, and the unique index.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
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
