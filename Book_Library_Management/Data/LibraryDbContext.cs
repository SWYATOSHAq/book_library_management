using Book_Library_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Book_Library_Management.Data;

public class LibraryDbContext : DbContext
{
    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<Author> Authors { get; set; } = null!;
    public DbSet<Genre> Genres { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=library.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Author ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.FirstName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(a => a.LastName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(a => a.Country)
                  .HasMaxLength(100);
        });

        // ── Genre ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasKey(g => g.Id);

            entity.Property(g => g.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(g => g.Description)
                  .HasMaxLength(500);
        });

        // ── Book ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(b => b.Id);

            entity.Property(b => b.Title)
                  .IsRequired()
                  .HasMaxLength(300);

            entity.Property(b => b.ISBN)
                  .HasMaxLength(20);

            // Книга → Автор (многие-к-одному), каскадное удаление
            entity.HasOne(b => b.Author)
                  .WithMany(a => a.Books)
                  .HasForeignKey(b => b.AuthorId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Книга → Жанр (многие-к-одному), каскадное удаление
            entity.HasOne(b => b.Genre)
                  .WithMany(g => g.Books)
                  .HasForeignKey(b => b.GenreId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
