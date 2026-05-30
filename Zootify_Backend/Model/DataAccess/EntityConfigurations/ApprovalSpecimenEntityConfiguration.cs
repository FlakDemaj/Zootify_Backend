using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Models.DataAccess.EntityConfigurations.Base;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

public class ApprovalSpecimenEntityConfiguration : BaseGuidEntityConfiguration<ApprovalSpecimenEntity>
{
    private string Table { get; }
    
    public ApprovalSpecimenEntityConfiguration()
    {
        Table = "approval_specimen";
        Schema = "zootify";
    }

    public override void Configure(EntityTypeBuilder<ApprovalSpecimenEntity> builder)
    {
        builder.ToTable(Table, Schema);
        
        base.Configure(builder);

        builder
            .Property(p => p.SpecimenGuid)
            .HasColumnName("specimen_guid")
            .IsRequired();
        
        builder
            .Property(p => p.ApprovalStatus)
            .HasColumnName("approval_status")
            .IsRequired();

        builder
            .Property(p => p.ApprovedByUserGuid)
            .HasColumnName("approved_by_user_guid");

        builder
            .Property(p => p.ApprovedAt)
            .HasColumnName("approved_at");
        
        builder
            .Property(p => p.IsTaken)
            .HasColumnName("is_taken")
            .IsRequired();

        builder
            .Property(p => p.Comment)
            .HasColumnName("comment")
            .HasMaxLength(250);
        
        builder.HasOne(p => p.ApprovedByUser)
            .WithMany(p => p.ApprovalSpecimens)
            .HasForeignKey(p => p.ApprovedByUserGuid)
            .OnDelete(DeleteBehavior.Cascade);

    }
}
