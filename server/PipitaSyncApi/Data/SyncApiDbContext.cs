using Microsoft.EntityFrameworkCore;
using PipitaSyncApi.Models;

namespace PipitaSyncApi.Data;

public sealed class SyncApiDbContext : DbContext
{
    public SyncApiDbContext(DbContextOptions<SyncApiDbContext> options)
        : base(options)
    {
    }

    public DbSet<SyncUser> SyncUsers => Set<SyncUser>();
    public DbSet<RemoteProfile> RemoteProfiles => Set<RemoteProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SyncUser>(entity =>
        {
            entity.ToTable("sync_users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(180).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(x => x.PasswordSalt).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<RemoteProfile>(entity =>
        {
            entity.ToTable("remote_profiles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(140).IsRequired();
            entity.Property(x => x.SnapshotFileName).HasMaxLength(255);
            entity.HasIndex(x => new { x.SyncUserId, x.Slug }).IsUnique();
            entity.HasOne(x => x.SyncUser)
                .WithMany(x => x.RemoteProfiles)
                .HasForeignKey(x => x.SyncUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
