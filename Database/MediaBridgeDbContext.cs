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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(x => x.Id);
        }
    }
}
