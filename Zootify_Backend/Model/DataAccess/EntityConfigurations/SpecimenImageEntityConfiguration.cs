using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

public class SpecimenImageEntityConfiguration : BaseGuidEntityConfiguration<SpecimenImageEntity>
{
    private string Table { get; }

    public SpecimenImageEntityConfiguration()
    {
        Table = "specimen_images";
        Schema = "zootify";
    }

    public override void Configure(EntityTypeBuilder<SpecimenImageEntity> builder)
    {
        builder.ToTable(Table, Schema);
        base.Configure(builder);

        builder
            .Property(p => p.SpecimenGuid)
            .HasColumnName("specimen_guid")
            .IsRequired();

        builder
            .Property(p => p.ImageBase64)
            .HasColumnName("image_url")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder
            .Property(p => p.UploadedAt)
            .HasColumnName("uploaded_at")
            .IsRequired();

        builder
            .HasOne(i => i.Specimen)
            .WithOne(s => s.Image)
            .HasForeignKey<SpecimenImageEntity>(i => i.SpecimenGuid)
            .OnDelete(DeleteBehavior.Cascade);
            
    }
}
