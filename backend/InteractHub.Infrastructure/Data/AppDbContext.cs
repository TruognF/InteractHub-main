using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using InteractHub.Application.Entities;

namespace InteractHub.Infrastructure.Data;
public class AppDbContext : IdentityDbContext<User>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Post> Posts {get; set;}
    public DbSet<Comment> Comments {get; set;}
    public DbSet<Like> Likes {get; set;}
    public DbSet<Friendship> Friendships {get; set;}
    public DbSet<Story> Stories {get; set;}
    public DbSet<Notification> Notifications {get; set;}
    public DbSet<Hashtag> Hashtags {get; set;}
    public DbSet<PostReport> PostReports {get; set;}  
    public DbSet<PostHashtag> PostHashtags {get; set;}
    public DbSet<PostDeletionLog> PostDeletionLogs {get; set;}
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMembership> GroupMemberships { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Message>(entity =>
        {
            entity.HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

}