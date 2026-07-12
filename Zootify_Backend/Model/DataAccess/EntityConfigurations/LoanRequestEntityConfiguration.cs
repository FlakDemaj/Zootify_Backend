using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

/// <summary>
/// EF Core configuration mapping <see cref="LoanRequestEntity"/> to the <c>zootify.loan_requests</c> table,
/// including the specimen, requester, and approver/rejecter relationships.
/// </summary>
public class LoanRequestEntityConfiguration : BaseGuidEntityConfiguration<LoanRequestEntity>
{
    /// <summary>The database table name the entity is mapped to.</summary>
    private string Table { get; }

    /// <summary>
    /// Initializes the configuration with the loan_requests table and zootify schema.
    /// </summary>
    public LoanRequestEntityConfiguration()
    {
        Table = "loan_requests";
        Schema = "zootify";
    }

    /// <summary>
    /// Configures the table mapping, columns, and the specimen/requester/approver foreign keys.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public override void Configure(EntityTypeBuilder<LoanRequestEntity> builder)
    {
        builder.ToTable(Table, Schema);
        base.Configure(builder);

        builder
            .Property(p => p.SpecimenGuid)
            .HasColumnName("specimen_guid")
            .IsRequired();

        builder
            .Property(p => p.RequesterGuid)
            .HasColumnName("requester_guid")
            .IsRequired();

        builder
            .Property(p => p.Status)
            .HasColumnName("status")
            .IsRequired();

        builder
            .Property(p => p.DesiredDueDate)
            .HasColumnName("desired_due_date")
            .IsRequired();

        builder
            .Property(p => p.RequesterNote)
            .HasColumnName("requester_note")
            .HasMaxLength(2000);

        builder
            .Property(p => p.RequestedAt)
            .HasColumnName("requested_at")
            .IsRequired();

        builder
            .Property(p => p.AdminApproverGuid)
            .HasColumnName("admin_approver_guid");

        builder
            .Property(p => p.AdminApprovedAt)
            .HasColumnName("admin_approved_at");

        builder
            .Property(p => p.CreatorApproverGuid)
            .HasColumnName("creator_approver_guid");

        builder
            .Property(p => p.CreatorApprovedAt)
            .HasColumnName("creator_approved_at");

        builder
            .Property(p => p.RejectedByGuid)
            .HasColumnName("rejected_by_guid");

        builder
            .Property(p => p.RejectedAt)
            .HasColumnName("rejected_at");

        builder
            .Property(p => p.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(2000);

        builder
            .Property(p => p.CreatedLoanGuid)
            .HasColumnName("created_loan_guid");

        builder
            .HasOne(l => l.Specimen)
            .WithMany(s => s.LoanRequests)
            .HasForeignKey(l => l.SpecimenGuid)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(l => l.Requester)
            .WithMany()
            .HasForeignKey(l => l.RequesterGuid)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Approver/rejecter columns reference users without an inverse navigation; NoAction keeps user
        // hard-deletes from cascading through requests (the purge job clears these references explicitly).
        builder
            .HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(l => l.AdminApproverGuid)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(l => l.CreatorApproverGuid)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(l => l.RejectedByGuid)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
