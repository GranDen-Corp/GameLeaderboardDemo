using Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Common
{
    public class GameContext : DbContext
    {
        public GameContext(DbContextOptions<GameContext> options) : base(options)
        {
        }

        public DbSet<Game> Games { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Summary> Summaries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Game>().ToTable("Games");
            modelBuilder.Entity<Player>().ToTable("Players");
            modelBuilder.Entity<Summary>().ToTable("Summaries")
                                            .HasKey(table => new
                                            {
                                                table.GameId,
                                                table.PlayerId
                                            });
        }
    }
}
