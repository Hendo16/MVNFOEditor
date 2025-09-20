using System;
using System.Linq;
using Avalonia.Styling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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
        public DbSet<AMVideoMetadata> AppleMusicVideoMetadata { get; set; }
        public DbSet<PsshKey> PsshKeys { get; set; }
        public DbSet<ArtistMetadata> ArtistMetadata { get; set; }
        public DbSet<AlbumMetadata> AlbumMetadata { get; set; }

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
                .HasMany(a => a.Metadata)
                .WithOne(am => am.Artist)
                .HasForeignKey(am => am.ArtistId)
                .HasPrincipalKey(a => a.Id)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Album>()
                .HasMany(a => a.Metadata)
                .WithOne(am => am.Album)
                .HasForeignKey(am => am.AlbumId)
                .HasPrincipalKey(a => a.Id)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ArtistMetadata>()
                .Property(a => a.SourceId)
                .HasConversion(
                    a => a.ToString(),
                    a => (SearchSource)Enum.Parse(typeof(SearchSource), a));
            
            modelBuilder.Entity<AlbumMetadata>()
                .Property(a => a.SourceId)
                .HasConversion(
                    a => a.ToString(),
                    a => (SearchSource)Enum.Parse(typeof(SearchSource), a));
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Other configurations if needed
            optionsBuilder.UseSqlite("Data Source=./Assets/MVNFOEditor.db;");
            base.OnConfiguring(optionsBuilder);
        }

        public bool Exists()
        {
            bool dbExists = Database.GetService<IRelationalDatabaseCreator>().Exists();
            return dbExists;
        }
    }
}