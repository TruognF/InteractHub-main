using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InteractHub.Application.Entities;

namespace InteractHub.Infrastructure.Configurations;
public class StoryConfig : IEntityTypeConfiguration<Story>
{
    public void Configure(EntityTypeBuilder<Story> builder)
    {
        builder.HasKey(s => s.Id);

        builder.HasOne(s => s.User).WithMany(u => u.Stories).HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}