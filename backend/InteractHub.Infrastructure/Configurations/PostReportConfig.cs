using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InteractHub.Application.Entities;

namespace InteractHub.Infrastructure.Configurations;
public class PostReportConfig : IEntityTypeConfiguration<PostReport>
{
    public void Configure(EntityTypeBuilder<PostReport> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Reason).IsRequired().HasConversion<int>();
        builder.Property(r => r.Status).IsRequired().HasConversion<int>();
        builder.Property(r => r.Detail).IsRequired(false);
        builder.Property(r => r.ReviewedByAdminId).IsRequired(false);
        builder.Property(r => r.ReviewedAt).IsRequired(false);

        builder.HasOne(r => r.Post).WithMany(p => p.Reports).HasForeignKey(r => r.PostId).OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(r => r.ReporterUser).WithMany().HasForeignKey(r => r.ReporterUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(r => r.ReviewedByAdmin).WithMany().HasForeignKey(r => r.ReviewedByAdminId).OnDelete(DeleteBehavior.NoAction);
    }
}