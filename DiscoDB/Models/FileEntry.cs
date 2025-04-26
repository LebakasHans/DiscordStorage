namespace DiscoDB.Models;

public class FileEntry
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public long Size { get; set; }

    public Guid FolderId { get; set; }
    public virtual required Folder Folder { get; set; }

    public List<ulong> MessageIds { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}
