using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.EntityConfigurations.Base;

public class BaseGuidEntityConfiguration<T> : IEntityTypeConfiguration<T>
where T: BaseGuidEntity
{
    protected string Schema;

    public BaseGuidEntityConfiguration()
    {
        Schema = "public";
    }
    
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder
            .Property(p => p.Guid)
            .IsRequired()
            .HasColumnName("guid")
            .ValueGeneratedNever();

        builder.HasKey(p => p.Guid);
    }
}