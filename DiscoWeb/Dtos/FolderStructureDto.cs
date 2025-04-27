namespace DiscoWeb.Dtos;

public class FolderStructureDto
{
    public required string Name { get; set; }
    public required Guid FolderId{ get; set; }
    public List<FolderStructureDto> SubFolders { get; set; } = [];
    public List<FileDto> Files { get; set; } = [];
}