using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

/// <summary>
/// EF Core configuration mapping <see cref="TaxonomyNodeEntity"/> to the <c>zootify.taxonomy_nodes</c> table,
/// including its self-referencing parent relationship and audit-user relationships.
/// </summary>
public class TaxonomyNodeEntityConfiguration : BaseCreationEntityConfiguration<TaxonomyNodeEntity>
{
    /// <summary>The database table name the entity is mapped to.</summary>
    private string Table { get; }

    /// <summary>
    /// Initializes the configuration with the taxonomy-nodes table and zootify schema.
    /// </summary>
    public TaxonomyNodeEntityConfiguration()
    {
        Table = "taxonomy_nodes";
        Schema = "zootify";
    }

    /// <summary>
    /// Configures the table mapping, columns, the parent/creator/approver relationships, and the unique index.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
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
            .Property(p => p.ApprovedByUserGuid)
            .HasColumnName("approved_by_user_guid");

        builder
            .Property(p => p.ApprovedDate)
            .HasColumnName("approved_date");

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
            .HasOne(t => t.ApprovedByUser)
            .WithMany(u => u.ApprovedTaxonomies)
            .HasForeignKey(t => t.ApprovedByUserGuid)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .HasIndex(t => new { t.ParentGuid, t.Name, t.Rank })
            .IsUnique();
    }
}
