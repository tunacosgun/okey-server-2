using Microsoft.EntityFrameworkCore;

namespace GameServer.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) {}

        public DbSet<Player> Players => Set<Player>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<RoomPlayer> RoomPlayers => Set<RoomPlayer>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Player>()
                .HasIndex(p => p.Username)
                .IsUnique();

            b.Entity<Room>()
                .HasIndex(r => r.Code)
                .IsUnique();

            b.Entity<RoomPlayer>()
                .HasIndex(rp => new { rp.RoomId, rp.Seat })
                .IsUnique(); // aynı oda, aynı koltuk 2 kez olmasın

            b.Entity<RoomPlayer>()
                .HasIndex(rp => new { rp.RoomId, rp.PlayerId })
                .IsUnique(); // oda + oyuncu benzersiz
        }
    }

    public class Player
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Room
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Code { get; set; } = default!;
        public int Capacity { get; set; } = 4;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // Oda–Oyuncu ilişkisi + koltuk
    public class RoomPlayer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RoomId { get; set; }
        public Guid PlayerId { get; set; }
        public int Seat { get; set; }           // 1..4
        public string? ConnectionId { get; set; } // (ops.) Hub ConnectionId
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}