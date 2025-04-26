using DiscoWeb.Dtos;
using DiscoWeb.Models;
using DiscoWeb.Queues;
using FluentResults;

namespace DiscoWeb.Services;

public class StorageService(ITaskQueue<StorageTask> queue, ILogger<StorageService> logger) : IStorageService
{
    public async Task<Result<FolderStructureDto>> GetFolderStructureAsync(string path, int depth)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<string>> CreateFolderAsync(string path)
    {
        var completionTask = await queue.EnqueueAsync(new FolderStorageTask
        {
            OperationType = FolderOperationType.Create,
            Path = path
        });

        var result = await completionTask;

        return ConvertToStringResult(result, "Failed to create folder");
    }

    public async Task<Result<string>> DeleteFolderAsync(string path)
    {
        var completionTask = await queue.EnqueueAsync(new FolderStorageTask
        {
            OperationType = FolderOperationType.Delete,
            Path = path
        });

        var result = await completionTask;

        return ConvertToStringResult(result, "Failed to delete folder");
    }

    public async Task<Result<string>> GetFileAsync(string path)
    {
        var completionTask = await queue.EnqueueAsync(new FileStorageTask
        {
            OperationType = FileOperationType.Read,
            Path = path
        });

        var result = await completionTask;

        return ConvertToStringResult(result, "Failed to get file");
    }

    public async Task<Result<string>> DeleteFileAsync(string path)
    {
        var completionTask = await queue.EnqueueAsync(new FileStorageTask
        {
            OperationType = FileOperationType.Delete,
            Path = path
        });

        var result = await completionTask;

        return ConvertToStringResult(result, "Failed to delete file");
    }

    public async Task<Result<string>> UploadFileAsync(string path, IFormFile file)
    {
        var completionTask = await queue.EnqueueAsync(new FileStorageTask
        {
            OperationType = FileOperationType.Upload,
            Path = path,
            Content = file
        });

        var result = await completionTask;

        return ConvertToStringResult(result, "Failed to upload file");
    }

    private Result<string> ConvertToStringResult(Result<object> result, string errorMessage)
    {
        try
        {
            if (!result.IsSuccess)
            {
                return Result.Fail<string>(result.Value?.ToString() ?? string.Empty);
            }
            
            return Result.Ok(result.Value?.ToString() ?? string.Empty);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ErrorPrefix}: Exception during conversion", errorMessage);
            return Result.Fail(errorMessage);
        }
    }
}
