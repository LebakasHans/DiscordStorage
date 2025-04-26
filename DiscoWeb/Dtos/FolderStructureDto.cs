namespace DiscoWeb.Dtos;

public class FolderStructureDto
{
    public required string Name { get; set; }
    public required string Path { get; set; }
    public List<FolderStructureDto> SubFolders { get; set; } = [];
    public List<FileDto> Files { get; set; } = [];
}