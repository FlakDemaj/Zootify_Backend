using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

/// <summary>
/// EF Core configuration mapping <see cref="SpecimenImageEntity"/> to the <c>zootify.specimen_images</c> table.
/// </summary>
public class SpecimenImageEntityConfiguration : BaseGuidEntityConfiguration<SpecimenImageEntity>
{
    /// <summary>The database table name the entity is mapped to.</summary>
    private string Table { get; }

    /// <summary>
    /// Initializes the configuration with the specimen-images table and zootify schema.
    /// </summary>
    public SpecimenImageEntityConfiguration()
    {
        Table = "specimen_images";
        Schema = "zootify";
    }

    /// <summary>
    /// Configures the table mapping and columns. The one-to-one relationship to the specimen is
    /// configured from the specimen side, which now owns the foreign key (<c>image_guid</c>).
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public override void Configure(EntityTypeBuilder<SpecimenImageEntity> builder)
    {
        builder.ToTable(Table, Schema);
        base.Configure(builder);

        builder
            .Property(p => p.ImageBase64)
            .HasColumnName("image_url")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder
            .Property(p => p.UploadedAt)
            .HasColumnName("uploaded_at")
            .IsRequired();
    }
}
