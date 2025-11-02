using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using MVNFOEditor.Models;

namespace MVNFOEditor.DB;

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
        modelBuilder.Entity<MusicVideo>()
            .HasMany(mv => mv.Genres)
            .WithMany(e => e.MusicVideos);

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
        var dbExists = Database.GetService<IRelationalDatabaseCreator>().Exists();
        return dbExists;
    }
}