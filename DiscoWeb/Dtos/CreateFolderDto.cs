namespace DiscoWeb.Dtos;

public class CreateFolderDto
{
    public required string Name { get; set; }
    public required Guid ParentId { get; set; }
}

public class FolderDto
{
    public required string Name { get; set; }
    public required Guid FolderId { get; set; }
}