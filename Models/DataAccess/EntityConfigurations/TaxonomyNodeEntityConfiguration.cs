using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Models.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Models.DataAccess.Entitys;

namespace Zootify_Backend.Models.DataAccess.EntityConfigurations;

public class TaxonomyNodeEntityConfiguration : BaseGuidEntityConfiguration<TaxonomyNodeEntity>
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
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder
            .Property(p => p.ApprovalStatus)
            .HasColumnName("approval_status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder
            .Property(p => p.ParentGuid)
            .HasColumnName("parent_guid");

        builder
            .Property(p => p.ProposedByUserGuid)
            .HasColumnName("proposed_by_user_guid");

        builder
            .HasOne(t => t.Parent)
            .WithMany(t => t.Children)
            .HasForeignKey(t => t.ParentGuid)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(t => t.ProposedByUser)
            .WithMany(u => u.ProposedTaxonomies)
            .HasForeignKey(t => t.ProposedByUserGuid)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasIndex(t => new { t.ParentGuid, t.Name, t.Rank })
            .IsUnique();
    }
}
