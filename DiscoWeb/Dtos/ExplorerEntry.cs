namespace DiscoWeb.Dtos;

public class ExplorerEntry
{
    public required string Name { get; set; }
    public required Guid Id { get; set; }
    public required string Path { get; set; }
    public long? Size { get; set; }
    public bool? Corrupted { get; set; }
    public string Type { get; set; } = "File";
    public bool IsFile { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}