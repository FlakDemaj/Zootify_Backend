using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Models.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Models.DataAccess.Entitys;

namespace Zootify_Backend.Models.DataAccess.EntityConfigurations;

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
            .Property(p => p.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(1000)
            .IsRequired();

        builder
            .Property(p => p.UploadedAt)
            .HasColumnName("uploaded_at")
            .IsRequired();

        builder
            .HasOne(i => i.Specimen)
            .WithMany(s => s.Images)
            .HasForeignKey(i => i.SpecimenGuid)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
