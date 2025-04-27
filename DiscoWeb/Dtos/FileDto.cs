namespace DiscoWeb.Dtos;

public class FileDto
{
    public required string Name { get; set; }
    public required Guid Id { get; set; }
    public long Size { get; set; }
    public bool Corrupted { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}