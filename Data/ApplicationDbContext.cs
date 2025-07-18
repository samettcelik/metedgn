using Microsoft.EntityFrameworkCore;
using DGN_BACK.Models;

namespace DGN_BACK.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<Image> Images { get; set; } = null!;
        public DbSet<MessageImage> MessageImages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Message configuration
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).HasMaxLength(1000);
                entity.Property(e => e.GondericiAdi).HasMaxLength(100).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // Image configuration
            modelBuilder.Entity<Image>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PublicId).HasMaxLength(200).IsRequired();
                entity.Property(e => e.OriginalUrl).HasMaxLength(500).IsRequired();
                entity.Property(e => e.OptimizedUrl).HasMaxLength(500);
                entity.Property(e => e.CroppedUrl).HasMaxLength(500);
                entity.Property(e => e.UploadedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // MessageImage configuration
            modelBuilder.Entity<MessageImage>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Configure MessageId foreign key explicitly
                entity.Property(e => e.MessageId).IsRequired();
                entity.HasOne(e => e.Message)
                    .WithMany(m => m.MessageImages)
                    .HasForeignKey(e => e.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Configure ImageId foreign key explicitly
                entity.Property(e => e.ImageId).IsRequired();
                entity.HasOne(e => e.Image)
                    .WithMany(i => i.MessageImages)
                    .HasForeignKey(e => e.ImageId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Ensure unique combination of MessageId and ImageId
                entity.HasIndex(e => new { e.MessageId, e.ImageId })
                    .IsUnique()
                    .HasDatabaseName("IX_MessageImages_MessageId_ImageId");
            });
        }
    }
} 