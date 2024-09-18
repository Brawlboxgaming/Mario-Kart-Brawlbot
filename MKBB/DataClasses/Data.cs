using Microsoft.EntityFrameworkCore;
using MKBB.Class;

namespace MKBB.Data
{
    public class MKBBContext : DbContext
    {
        public DbSet<CouncilMemberData> Council { get; set; }
        public DbSet<PlayerData> Players { get; set; }
        public DbSet<ServerData> Servers { get; set; }
        public DbSet<ToolData> Tools { get; set; }
        public DbSet<TrackData> Tracks { get; set; }
        public DbSet<OldTrackData> OldTracks { get; set; }
        public DbSet<GBTrackData> GBTracks { get; set; }
        public DbSet<GBTimeData> GBTimes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
#if DEBUG
            string db = "testMKBB";
#else
            string db = "MKBB";
#endif
            options.UseSqlServer(Util.GetDBConnectionString(db));
            options.EnableSensitiveDataLogging(true);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CouncilMemberData>().ToTable("Council");
            modelBuilder.Entity<PlayerData>().ToTable("Players");
            modelBuilder.Entity<ServerData>().ToTable("Servers");
            modelBuilder.Entity<ToolData>().ToTable("Tools");
            modelBuilder.Entity<TrackData>().ToTable("Tracks");
            modelBuilder.Entity<OldTrackData>().ToTable("OldTracks");
            modelBuilder.Entity<GBTrackData>().ToTable("GBTracks");
            modelBuilder.Entity<GBTimeData>().ToTable("GBTimes");
        }
    }
}