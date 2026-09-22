using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InteractHub.Application.Entities;

namespace InteractHub.Infrastructure.Configurations;

public class MessageConfig : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasIndex(m => new { m.SenderId, m.ReceiverId, m.CreatedAt });
        builder.HasIndex(m => m.GroupId);
    }
}