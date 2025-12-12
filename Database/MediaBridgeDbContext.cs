using MediaBridge.Database.DB_Models;
using Microsoft.EntityFrameworkCore;

namespace MediaBridge.Database
{
    public class MediaBridgeDbContext : DbContext
    {
        public MediaBridgeDbContext(DbContextOptions<MediaBridgeDbContext> options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> User_Roles { get; set; }
        public DbSet<Config> Configs { get; set; }
        public DbSet<CachedData> CachedData { get; set; }
        public DbSet<MediaRequestLog> MediaRequestLogs { get; set; }
        public DbSet<DownloadRequests> DownloadRequests { get; set; }
        public DbSet<DownloadedMovies> DownloadedMovies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CachedData>().ToTable("cached_data");
            modelBuilder.Entity<MediaRequestLog>().ToTable("media_request_logs");
            modelBuilder.Entity<DownloadRequests>().ToTable("download_requests");
            modelBuilder.Entity<DownloadedMovies>().ToTable("downloaded_movies");

            // User configuration
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<UserRole>()
                .HasKey(y => y.Id);

            modelBuilder.Entity<Role>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRole)
                .HasForeignKey(r => r.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Config configuration
            modelBuilder.Entity<Config>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<Config>()
                .HasIndex(c => c.Key)
                .IsUnique();

            // CachedData configuration
            modelBuilder.Entity<CachedData>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<CachedData>()
                .HasIndex(c => c.CacheKey)
                .IsUnique();

            modelBuilder.Entity<CachedData>()
                .HasIndex(c => c.ExpiresAt);

            // MediaRequestLog configuration
            modelBuilder.Entity<MediaRequestLog>()
                .HasKey(m => m.Id);

            modelBuilder.Entity<MediaRequestLog>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MediaRequestLog>()
                .HasIndex(m => m.UserId);

            modelBuilder.Entity<MediaRequestLog>()
                .HasIndex(m => m.RequestedAt);

            modelBuilder.Entity<MediaRequestLog>()
                .HasIndex(m => m.MediaType);

            // DownloadRequests configuration
            modelBuilder.Entity<DownloadRequests>()
                .HasKey(d => d.Id);

            modelBuilder.Entity<DownloadRequests>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // DownloadedMovies configuration
            modelBuilder.Entity<DownloadedMovies>()
                .HasKey(dm => dm.Id);
        }
    }
}
