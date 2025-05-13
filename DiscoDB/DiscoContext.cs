using DiscoDB.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DiscoDB;

public class DiscoContext(DbContextOptions<DiscoContext> options) : DbContext(options)
{
    public DbSet<Folder> Folders { get; set; }
    public DbSet<Models.FileEntry> Files { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Folder>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Name).IsRequired();
            entity.HasIndex(f => new { f.ParentFolderId, f.Name }).IsUnique();

            entity.HasOne(f => f.ParentFolder)
                  .WithMany(f => f.ChildFolders)
                  .HasForeignKey(f => f.ParentFolderId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(f => f.Files)
                  .WithOne(file => file.Folder)
                  .HasForeignKey(file => file.FolderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FileEntry>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Name).IsRequired();
            entity.HasIndex(f => new { f.FolderId, f.Name }).IsUnique();

            entity.Property(f => f.MessageIds)
                  .HasConversion(
                      v => string.Join(',', v),
                      v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(ulong.Parse).ToList()
                  )
                  .Metadata.SetValueComparer(new ValueComparer<List<ulong>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));

        });
    }
}