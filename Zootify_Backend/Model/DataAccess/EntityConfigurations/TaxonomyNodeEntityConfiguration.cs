using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Models.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Models.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

public class TaxonomyNodeEntityConfiguration : BaseDeletionEntityConfiguration<TaxonomyNodeEntity>
{
    private string Table { get; }

    public TaxonomyNodeEntityConfiguration()
    {
        Table = "taxonomy_nodes";
        Schema = "zootify";
    }

    public override void Configure(EntityTypeBuilder<TaxonomyNodeEntity> builder)
    {
        builder.ToTable(Table, Schema);
        base.Configure(builder);

        builder
            .Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder
            .Property(p => p.Rank)
            .HasColumnName("rank")
            .IsRequired();

        builder
            .Property(p => p.ParentGuid)
            .HasColumnName("parent_guid");
        
        builder
            .Property(p => p.ApprovalStatus)
            .HasColumnName("approval_status");

        builder
            .HasOne(t => t.Parent)
            .WithMany(t => t.Children)
            .HasForeignKey(t => t.ParentGuid)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(t => t.CreatedByUser)
            .WithMany(u => u.CreatedTaxonomies)
            .HasForeignKey(t => t.CreatedByUserGuid)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder
            .HasOne(t => t.UpdatedByUser)
            .WithMany(u => u.UpdatedTaxonomies)
            .HasForeignKey(t => t.UpdatedByUserGuid)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder
            .HasOne(t => t.DeletedByUser)
            .WithMany(u => u.DeletedTaxonomies)
            .HasForeignKey(t => t.DeletedByUserGuid)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .HasIndex(t => new { t.ParentGuid, t.Name, t.Rank })
            .IsUnique();
    }
}
