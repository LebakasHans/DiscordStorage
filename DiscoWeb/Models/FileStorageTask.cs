namespace DiscoWeb.Models;

public class FileStorageTask : StorageTask
{
    public FileOperationType OperationType { get; set; }
    public IFormFile? Content { get; set; }
}
