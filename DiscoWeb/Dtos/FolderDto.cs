namespace DiscoWeb.Dtos;

public class FolderDto
{
    public required string Name { get; set; }
    public required Guid ParentId { get; set; }
}