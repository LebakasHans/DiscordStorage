namespace DiscoWeb.Models;

public class FilePart
{
    public int Index { get; set; }
    public byte[] Content { get; set; } = [];
    public string RawFileName { get; set; } = string.Empty;
}
