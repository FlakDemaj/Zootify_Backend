using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Models.DataAccess.EntityConfigurations.Base;

namespace Zootify_Backend.Models.DataAccess.EntityConfigurations;

public class LoanEntityConfiguration : BaseGuidEntityConfiguration<LoanEntity>
{
    private string Table { get; }

    public LoanEntityConfiguration()
    {
        Table = "loans";
        Schema = "zootify";
    }

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
