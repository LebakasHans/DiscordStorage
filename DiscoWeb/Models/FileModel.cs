namespace DiscoWeb.Models;

public class FileModel
{
    public required string FileName { get; set; }
    public required byte[] Content { get; set; }
}