using DiscoWeb.Errors;
using DiscoWeb.Models;
using DiscoWeb.Queues;
using DiscoWeb.Services.Processors;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using NetCord;
using NetCord.Rest;

namespace DiscoWeb.BackgroundServices;

public class DiscordBotWorker(
    ITaskQueue<StorageTask> queue,
    ILogger<DiscordBotWorker> logger,
    RestClient restClient,
    FileTaskProcessor fileProcessor,
    FolderTaskProcessor folderProcessor)
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
            logger.LogInformation("Processing {TaskType} storage task ({OperationType}) for path: {Path}", taskType, operationType, storageTask.Path);

            try
            {
                Result<object> result = storageTask switch
                {
                    FolderStorageTask folderTask => await folderProcessor.ProcessAsync(folderTask),
                    FileStorageTask fileTask => await fileProcessor.ProcessAsync(fileTask),
                    _ => Result.Fail(new InternalServerError("Unknown task type"))
                };

                storageTask.CompletionSource.SetResult(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing {TaskType} storage task ({OperationType}) for path: {Path}",
                    taskType, operationType, storageTask.Path); 
                storageTask.CompletionSource.SetResult(Result.Fail(new InternalServerError($"Error processing {taskType} operation: {operationType}").CausedBy(ex)));
            }

            if (!storageTask.CompletionSource.Task.IsCompleted)
            {
                logger.LogWarning("Task {TaskType} storage task ({OperationType}) for path: {Path} did not complete", taskType, operationType, storageTask.Path);
                storageTask.CompletionSource.SetResult(Result.Fail(new InternalServerError($"Task {taskType} operation: {operationType} did not complete")));
            }
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
