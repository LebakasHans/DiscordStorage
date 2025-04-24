namespace DiscoWeb.Dtos;

public class FolderStructureDto
{
    public required string Name { get; set; }
    public required string Path { get; set; }
    public List<FolderStructureDto> SubFolders { get; set; } = [];
    public List<FileDto> Files { get; set; } = [];
}

public class FileDto
{
    public required string Name { get; set; }
    public required string Path { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
}