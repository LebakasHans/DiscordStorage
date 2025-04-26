using DiscoWeb.Helper.Discord;
using DiscoWeb.Models;
using FluentResults;
using NetCord.Rest;

namespace DiscoWeb.Services.Processors;

public class FileTaskProcessor(RestClient restClient, ILogger<FileTaskProcessor> logger)
{
    public async Task<Result<object>> ProcessAsync(FileStorageTask fileTask)
    {
        switch (fileTask.OperationType)
        {
            case FileOperationType.Read:
                return await ProcessReadOperationAsync();
            case FileOperationType.Upload:
                return await ProcessUploadOperationAsync(fileTask.Content!);
            case FileOperationType.Delete:
                return await ProcessDeleteOperationAsync();
            default:
                return Result.Fail("Invalid Operation type");
        }
    }

    private async Task<Result<object>> ProcessReadOperationAsync()
    {
        var result = await FileStorageHelper.ReadFile([1365044815708029070], restClient);

        return result;
    }

    private async Task<Result<object>> ProcessUploadOperationAsync(string content)
    {
        return await FileStorageHelper.WriteFile(content, restClient);
    }

    private async Task<Result<object>> ProcessDeleteOperationAsync()
    {
        logger.LogInformation("Delete operation requested");
        return Result.Ok<object>("Delete operation not implemented");
    }
}
