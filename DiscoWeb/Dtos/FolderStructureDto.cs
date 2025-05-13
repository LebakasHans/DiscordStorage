namespace DiscoWeb.Dtos;

public class FolderStructureDto
{
    public required string Name { get; set; }
    public required Guid FolderId{ get; set; }
    public Guid? ParentFolderId { get; set; }
    public required string Path { get; set; }
    public List<ExplorerEntry> Entries { get; set; } = [];
}