using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations;

public class TagEntityConfiguration : BaseGuidEntityConfiguration<TagEntity>
{
    private string Table { get; }
    
    public TagEntityConfiguration()
    {
        Table = "tags";
        Schema = "zootify";
    }

    public override void Configure(EntityTypeBuilder<TagEntity> builder)
    {
        builder.ToTable(Table, Schema);
        
        base.Configure(builder);
        
        builder
            .Property(p => p.Tag)
            .HasColumnName("tag")
            .HasColumnType("varchar")
            .HasMaxLength(100)
            .IsRequired();
    }
}