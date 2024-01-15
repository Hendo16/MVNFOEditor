using Microsoft.EntityFrameworkCore;
using MVNFOEditor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVNFOEditor.DB
{
    public class MusicDbContext : DbContext
    {
        public DbSet<MusicVideo> MusicVideos { get; set; }
        public DbSet<Genre> Genres { get; set; }

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
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=MVNFOEditor.db");
        }
    }
}
