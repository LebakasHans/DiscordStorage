using DiscoWeb.Errors;
using DiscoWeb.Helper.Discord;
using DiscoWeb.Models;
using DiscoWeb.Queues;
using FluentResults;
using NetCord;
using NetCord.Rest;

namespace DiscoWeb.BackgroundServices;

public class DiscordBotWorker(
    ITaskQueue<StorageTask> queue,
    ILogger<DiscordBotWorker> logger,
    RestClient restClient)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var currentUser = await restClient.GetCurrentUserAsync(cancellationToken: stoppingToken);
            logger.LogInformation("Logged in as {Username}#{Discriminator}",
                currentUser.Username,
                currentUser.Discriminator);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect Discord bot");
            return;
        }

        await StarProcessingLoopAsync(stoppingToken);
    }

    private async Task StarProcessingLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var storageTask = await queue.DequeueAsync(stoppingToken);

            var (taskType, operationType) = GetTaskInfo(storageTask);
            logger.LogInformation("Processing {TaskType} storage task ({OperationType}) for path: {Path}", 
                taskType, operationType, storageTask.Path);

            try
            {
                switch (storageTask)
                {
                    case FolderStorageTask folderTask:
                        await ProcessFolderTask(folderTask);
                        break;
                    case FileStorageTask fileTask:
                        await ProcessFileTask(fileTask);
                        break;
                    default:
                        storageTask.CompletionSource.SetResult(Result.Fail(new InternalServerError("Unknown task type")));
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing {TaskType} storage task ({OperationType}) for path: {Path}",
                    taskType, operationType, storageTask.Path);
                storageTask.CompletionSource.SetResult(Result.Fail(new InternalServerError($"Error processing {taskType} operation: {operationType}")));
            }

            if (!storageTask.CompletionSource.Task.IsCompleted)
            {
                logger.LogWarning("TaskCompletionSource not completed after processing {TaskType} task ({OperationType}) for path: {Path}",
                    taskType, operationType, storageTask.Path);
                storageTask.CompletionSource.SetResult(Result.Fail($"Something went wrong with {taskType} operation: {operationType}"));
            }
        }
    }

    private async Task ProcessFolderTask(FolderStorageTask folderTask)
    {
        switch (folderTask.OperationType)
        {
            case FolderOperationType.Read:
                Console.WriteLine("Read");
                break;
            case FolderOperationType.Delete:
                Console.WriteLine("Delete");
                break;
            case FolderOperationType.Create:
                Console.WriteLine("Create");
                break;
            default:
                folderTask.CompletionSource.SetResult(Result.Fail("Invalid Operation type"));
                break;
        }
    }

    private async Task ProcessFileTask(FileStorageTask fileTask)
    {
        switch (fileTask.OperationType)
        {
            case FileOperationType.Read:
                Console.WriteLine("read");
                break;
            case FileOperationType.Upload:
                await FileStorageHelper.WriteFile(fileTask.Content!, restClient);
                break;
            case FileOperationType.Delete:
                Console.WriteLine("Delete");
                break;
            default:
                fileTask.CompletionSource.SetResult(Result.Fail("Invalid Operation type"));
                break;
        }
    }

    private static (string taskType, string operationType) GetTaskInfo(StorageTask storageTask)
    {
        string taskType = storageTask is FileStorageTask ? "File" : "Folder";
        string operationType = storageTask switch
        {
            FileStorageTask fileTask => fileTask.OperationType.ToString(),
            FolderStorageTask folderTask => folderTask.OperationType.ToString(),
            _ => "Unknown"
        };

        return (taskType, operationType);
    }
}
