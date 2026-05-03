using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Models;

namespace Zootify_Backend.Data;

public class ZootifyDbContext : DbContext
{
    public ZootifyDbContext(DbContextOptions<ZootifyDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<TaxonomyNode> TaxonomyNodes => Set<TaxonomyNode>();
    public DbSet<GeographicLocation> GeographicLocations => Set<GeographicLocation>();
    public DbSet<Specimen> Specimens => Set<Specimen>();
    public DbSet<SpecimenImage> SpecimenImages => Set<SpecimenImage>();
    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ──
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);
            entity.Property(u => u.PasswordHash).HasMaxLength(512);
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            entity.Property(u => u.Status).HasConversion<string>().HasMaxLength(20);
        });

        // ── TaxonomyNode (SCOFGA-Hierarchie) ──
        modelBuilder.Entity<TaxonomyNode>(entity =>
        {
            entity.Property(t => t.Name).HasMaxLength(256);
            entity.Property(t => t.Rank).HasConversion<string>().HasMaxLength(20);
            entity.Property(t => t.ApprovalStatus).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(t => t.Parent)
                .WithMany(t => t.Children)
                .HasForeignKey(t => t.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.ProposedByUser)
                .WithMany(t => t.ProposedTaxonomies)
                .HasForeignKey(t => t.ProposedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(t => new { t.ParentId, t.Name, t.Rank }).IsUnique();
        });

        // ── GeographicLocation ──
        modelBuilder.Entity<GeographicLocation>(entity =>
        {
            entity.Property(g => g.Name).HasMaxLength(256);
            entity.Property(g => g.Level).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(g => g.Parent)
                .WithMany(g => g.Children)
                .HasForeignKey(g => g.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(g => new { g.ParentId, g.Name }).IsUnique();
        });

        // ── Specimen ──
        modelBuilder.Entity<Specimen>(entity =>
        {
            entity.HasOne(s => s.Species)
                .WithMany(t => t.Specimens)
                .HasForeignKey(s => s.SpeciesId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.GeographicLocation)
                .WithMany(g => g.Specimens)
                .HasForeignKey(s => s.GeographicLocationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(s => s.Owner)
                .WithMany(u => u.Specimens)
                .HasForeignKey(s => s.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(s => s.StorageBuilding).HasMaxLength(200);
            entity.Property(s => s.StorageCabinet).HasMaxLength(200);
            entity.Property(s => s.StorageShelf).HasMaxLength(200);
            entity.Property(s => s.StorageCompartment).HasMaxLength(200);
            entity.Property(s => s.Comment).HasMaxLength(2000);

            entity.HasIndex(s => s.IsPublic);
            entity.HasIndex(s => s.OwnerId);
            entity.HasIndex(s => s.SpeciesId);
        });

        // ── SpecimenImage ──
        modelBuilder.Entity<SpecimenImage>(entity =>
        {
            entity.Property(i => i.ImageUrl).HasMaxLength(1000);

            entity.HasOne(i => i.Specimen)
                .WithMany(s => s.Images)
                .HasForeignKey(i => i.SpecimenId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Loan ──
        modelBuilder.Entity<Loan>(entity =>
        {
            entity.Property(l => l.BorrowerExternalName).HasMaxLength(256);
            entity.Property(l => l.Notes).HasMaxLength(2000);

            entity.HasOne(l => l.Specimen)
                .WithMany(s => s.Loans)
                .HasForeignKey(l => l.SpecimenId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.Lender)
                .WithMany(u => u.LoansAsLender)
                .HasForeignKey(l => l.LenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.Borrower)
                .WithMany(u => u.LoansAsBorrower)
                .HasForeignKey(l => l.BorrowerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
