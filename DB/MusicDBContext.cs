using System.Linq;
using Avalonia.Styling;
using Microsoft.EntityFrameworkCore;
using MVNFOEditor.Models;
using MVNFOEditor.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SukiUI.Models;

namespace MVNFOEditor.DB
{
    public class MusicDbContext : DbContext
    {
        public DbSet<MusicVideo> MusicVideos { get; set; }
        public DbSet<Artist> Artist { get; set; }
        public DbSet<Album> Album { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<SettingsData> SettingsData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MusicVideoGenre>()
                .HasKey(mvg => new { mvg.MusicVideoID, mvg.GenreID });

            modelBuilder.Entity<MusicVideoGenre>()
                .HasOne(mvg => mvg.MusicVideo)
                .WithMany(mv => mv.MusicVideoGenres)
                .HasForeignKey(mvg => mvg.MusicVideoID);

            modelBuilder.Entity<MusicVideoGenre>()
                .HasOne(mvg => mvg.Genre)
                .WithMany(g => g.MusicVideoGenres)
                .HasForeignKey(mvg => mvg.GenreID);

            modelBuilder.Entity<Artist>()
                .Property(a => a.YTMusicAlbumResults)
                .HasColumnType("jsonb");

            modelBuilder.Entity<Artist>()
                .Property(a => a.YTMusicAlbumResults)
                .HasConversion(
                    a => a.ToString(),
                    a => JArray.Parse(a));

            modelBuilder.Entity<SettingsData>()
                .Property(t => t.Theme)
                .HasConversion(
                    a => JsonConvert.SerializeObject(a),
                    a => JsonConvert.DeserializeObject<SukiColorTheme>(a));

            modelBuilder.Entity<SettingsData>()
                .Property(t => t.LightOrDark)
                .HasConversion(
                    a => JsonConvert.SerializeObject(a),
                    a => JsonConvert.DeserializeObject<ThemeVariant>(a));
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Other configurations if needed
            optionsBuilder.UseSqlite("Data Source=MVNFOEditor.db;");
            base.OnConfiguring(optionsBuilder);
        }
    }
}
