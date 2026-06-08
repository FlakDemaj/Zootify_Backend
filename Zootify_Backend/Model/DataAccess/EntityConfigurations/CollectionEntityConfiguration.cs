using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

public class CollectionEntityConfiguration : BaseDeletionEntityConfiguration<CollectionEntity>
{
    private string Table { get; }

    public CollectionEntityConfiguration()
    {
        Table = "collections";
        Schema = "zootify";
    }

    public override void Configure(EntityTypeBuilder<CollectionEntity> builder)
    {
        builder.ToTable(Table, Schema);
        
        base.Configure(builder);
        
        builder
            .Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder
            .Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(400);
        
        builder
            .Property(e => e.IsPrivate)
            .HasColumnName("private")
            .IsRequired();
    }
    
}