namespace DiscoDB.Models;

public class Folder
{
    public Guid Id { get; set; }
    public required string Name { get; set; }

    public Guid? ParentFolderId { get; set; }
    public Folder? ParentFolder { get; set; }

    public virtual ICollection<Folder> ChildFolders { get; set; } = [];
    public virtual ICollection<FileEntry> Files { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
