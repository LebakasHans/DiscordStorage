using DiscoWeb.Models;
using FluentResults;
using NetCord.Rest;

namespace DiscoWeb.Services.Processors;

public class FolderTaskProcessor(RestClient restClient, ILogger<FolderTaskProcessor> logger)
{
    public async Task<Result<object>> ProcessAsync(FolderStorageTask folderTask)
    {
        switch (folderTask.OperationType)
        {
            case FolderOperationType.Read:
                Console.WriteLine("Read");
                return Result.Ok<object>("Read operation not implemented");
            case FolderOperationType.Delete:
                Console.WriteLine("Delete");
                return Result.Ok<object>("Delete operation not implemented");
            case FolderOperationType.Create:
                Console.WriteLine("Create");
                return Result.Ok<object>("Create operation not implemented");
            default:
                return Result.Fail("Invalid Operation type");
        }
    }
}
