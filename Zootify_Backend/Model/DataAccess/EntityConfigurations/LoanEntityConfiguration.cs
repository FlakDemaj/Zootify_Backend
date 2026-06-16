using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

/// <summary>
/// EF Core configuration mapping <see cref="LoanEntity"/> to the <c>zootify.loans</c> table, including
/// the specimen, lender, and borrower relationships.
/// </summary>
public class LoanEntityConfiguration : BaseGuidEntityConfiguration<LoanEntity>
{
    /// <summary>The database table name the entity is mapped to.</summary>
    private string Table { get; }

    /// <summary>
    /// Initializes the configuration with the loans table and zootify schema.
    /// </summary>
    public LoanEntityConfiguration()
    {
        Table = "loans";
        Schema = "zootify";
    }

    /// <summary>
    /// Configures the table mapping, columns, and the specimen/lender/borrower foreign keys.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public override void Configure(EntityTypeBuilder<LoanEntity> builder)
    {
        builder.ToTable(Table, Schema);
        base.Configure(builder);

        builder
            .Property(p => p.SpecimenGuid)
            .HasColumnName("specimen_guid")
            .IsRequired();

        builder
            .Property(p => p.LenderGuid)
            .HasColumnName("lender_guid")
            .IsRequired();

        builder
            .Property(p => p.BorrowerGuid)
            .HasColumnName("borrower_guid")
            .IsRequired();

        builder
            .Property(p => p.LoanDate)
            .HasColumnName("loan_date")
            .IsRequired();

        builder
            .Property(p => p.DueDate)
            .HasColumnName("due_date")
            .IsRequired();

        builder
            .Property(p => p.ReturnDate)
            .HasColumnName("return_date");

        builder
            .Property(p => p.Notes)
            .HasColumnName("notes")
            .HasMaxLength(2000);

        builder
            .Property(p => p.Stage1ReminderSentAt)
            .HasColumnName("stage1_reminder_sent_at");

        builder
            .Property(p => p.Stage2ReminderSentAt)
            .HasColumnName("stage2_reminder_sent_at");

        builder
            .Property(p => p.Stage3ReminderSentAt)
            .HasColumnName("stage3_reminder_sent_at");
        
        builder
            .HasOne(l => l.Specimen)
            .WithMany(s => s.Loans)
            .HasForeignKey(l => l.SpecimenGuid)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(l => l.Lender)
            .WithMany(u => u.LoansAsLender)
            .HasForeignKey(l => l.LenderGuid)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(l => l.Borrower)
            .WithMany(u => u.LoansAsBorrower)
            .HasForeignKey(l => l.BorrowerGuid)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
