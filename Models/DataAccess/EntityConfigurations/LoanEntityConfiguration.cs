using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Models.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Models.DataAccess.Entitys;

namespace Zootify_Backend.Models.DataAccess.EntityConfigurations;

public class LoanEntityConfiguration : BaseGuidEntityConfiguration<LoanEntity>
{
    private string Table { get; }

    public LoanEntityConfiguration()
    {
        Table = "loans";
        Schema = "public";
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
            .HasColumnName("borrower_guid");

        builder
            .Property(p => p.BorrowerExternalName)
            .HasColumnName("borrower_external_name")
            .HasMaxLength(256);

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
            .OnDelete(DeleteBehavior.Restrict);
    }
}
