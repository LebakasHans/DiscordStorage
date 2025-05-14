using DiscoWeb.Errors;
using DiscoWeb.Models;
using FluentResults;
using NetCord.Gateway;
using NetCord.Rest;
using System.Diagnostics;
using System.Text.RegularExpressions;
using NetCord;

namespace DiscoWeb.Discord;

public class DiscordFileStorage(RestClient restClient, ILogger<DiscordFileStorage> logger, HttpClient httpClient) : IDiscordFileStorage
{
    private const ulong ChannelId = 1313525215837294644;
    private const long MaxChunkSize = 8 * 1024 * 1024;

    public async Task<Result<List<ulong>>> WriteFileAsync(IFormFile file)
    {
        logger.LogInformation("Attempting to upload file {FileName} of size {FileSize} bytes.", file.FileName, file.Length);
        var stopwatch = Stopwatch.StartNew();


        await using var stream = file.OpenReadStream();
        var readBuffer = new byte[MaxChunkSize];
        int partNumber = 1;

        List<Task<RestMessage>> uploadTasks = [];
        var streamsToDispose = new List<MemoryStream>();


        while (true)
        {
            var attachments = new List<AttachmentProperties>();

            for (int i = 0; i < 10; i++) 
            {
                int bytesReadFromFile = await stream.ReadAsync(readBuffer);
                if (bytesReadFromFile == 0)
                    break;

                var partData = new byte[bytesReadFromFile];
                Array.Copy(readBuffer, 0, partData, 0, bytesReadFromFile);

                var chunkStream = new MemoryStream(partData);
                streamsToDispose.Add(chunkStream);

                var partFileName = $"{file.FileName}.part{partNumber}";
                attachments.Add(new AttachmentProperties(partFileName, chunkStream));
                partNumber++;
            }

            if (attachments.Count == 0)
                break;

            var messageProperties = new MessageProperties().WithAttachments(attachments);

            uploadTasks.Add(restClient.SendMessageAsync(ChannelId, messageProperties));
        }

        await Task.WhenAll(uploadTasks);

        var messageIds = uploadTasks.Select(task => task.Result.Id).ToList();

        foreach (var ms in streamsToDispose)
        {
            await ms.DisposeAsync();
        }

        stopwatch.Stop();
        logger.LogInformation("FileEntry {FileName} uploaded successfully in {MessageCount} messages. Time taken: {ElapsedMilliseconds} ms.",
        file.FileName, messageIds.Count, stopwatch.ElapsedMilliseconds);
        return Result.Ok(messageIds);
    }

    private async Task<Result<List<FilePart>>> DownloadFilePartAsync(ulong messageId, int index, int totalCount)
    {
        try
        {
            logger.LogDebug("Starting task for message {MessageId} (Part {IndexPlusOne}/{Total}).", messageId, index + 1, totalCount);

            RestMessage message;
            try
            {
                message = await restClient.GetMessageAsync(ChannelId, messageId);
            }
            catch (RestException ex)
            {
                logger.LogWarning(ex, "RestException while fetching message {MessageId} for part {IndexPlusOne}.", messageId, index + 1);
                return Result.Fail(new InternalServerError($"Failed to fetch message details for {messageId}. Discord API error: {ex.Message}").CausedBy(ex));
            }

            if (message.Attachments.Count == 0)
            {
                var errorMsg = $"Message {messageId} contains no attachments.";
                logger.LogWarning(errorMsg);
                return Result.Fail(new InternalServerError(errorMsg));
            }

            List<Task<Result<FilePart>>> downloadTasks = [];

            for (int i = 0; i < message.Attachments.Count; i++)
            {
                var task = DownloadAttachment(message.Attachments[i], index + i);
                downloadTasks.Add(task);
            }

            await Task.WhenAll(downloadTasks);

            var results = downloadTasks.Select(x => x.Result).ToList();

            if (results.Any(x => x.IsFailed))
            {
                return Result.Fail(new InternalServerError("Failed to download Message")).WithErrors(results.SelectMany(x => x.Errors).ToList());
            }

            var parts = results.Select(x => x.Value).ToList();

            return Result.Ok(parts);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error processing message {MessageId} for part {IndexPlusOne}.", messageId, index + 1);
            return Result.Fail(new InternalServerError($"Unexpected error processing part for message {messageId}.").CausedBy(ex));
        }
    }

    private async Task<Result<FilePart>> DownloadAttachment(Attachment attachment, int index)
    {
        logger.LogInformation("Downloading attachment {AttachmentFileName} from URL {AttachmentUrl}.", attachment.FileName, attachment.Url);

        using var response = await httpClient.GetAsync(attachment.Url, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            var errorMsg = $"Failed to download attachment {attachment.FileName}. HTTP status code: {response.StatusCode}";
            logger.LogWarning(errorMsg);
            return Result.Fail(new InternalServerError(errorMsg));
        }

        var contentBytes = await response.Content.ReadAsByteArrayAsync();

        logger.LogInformation("Finished downloading part {AttachmentFileName}.", attachment.FileName);

        return Result.Ok(new FilePart
        {
            Index = index,
            Content = contentBytes,
            RawFileName = attachment.FileName
        });
    }

    private async Task<FileModel> AssembleFileFromPartsAsync(IEnumerable<FilePart> successfulParts, string defaultFileNameOnError)
    {
        var orderedParts = successfulParts.OrderBy(p => p.Index).ToList();
        string originalFileName = defaultFileNameOnError;

        if (orderedParts.Count != 0)
        {
            var firstPartFileName = orderedParts[0].RawFileName;
            originalFileName = Regex.Replace(firstPartFileName, @"\.part\d+$", "");
            if (string.IsNullOrEmpty(originalFileName))
            {
                logger.LogWarning("Could not extract original filename from {AttachmentFileName}. Using attachment name as fallback.", firstPartFileName);
                originalFileName = firstPartFileName;
            }
        }

        var combinedStream = new MemoryStream();
        foreach (var part in orderedParts)
        {
            await combinedStream.WriteAsync(part.Content, 0, part.Content.Length);
        }
        combinedStream.Position = 0;
        var fileBytes = combinedStream.ToArray();
        await combinedStream.DisposeAsync();

        return new FileModel
        {
            FileName = originalFileName,
            Content = fileBytes
        };
    }

    public async Task<Result<FileModel>> ReadFileAsync(List<ulong> messageIds)
    {
        logger.LogInformation("Attempting to read file from {MessageCount} messages.", messageIds.Count);

        if (messageIds.Count == 0)
        {
            logger.LogWarning("No message IDs provided for reading file.");
            return Result.Fail(new ValidationError("No message IDs provided"));
        }

        var tasks = messageIds.Select((messageId, index) => DownloadFilePartAsync(messageId, index, messageIds.Count)).ToList();

        var allTaskResults = await Task.WhenAll(tasks);

        var failedTasksResults = allTaskResults.Where(r => r.IsFailed).ToList();
        if (failedTasksResults.Count != 0)
        {
            var aggregatedErrors = failedTasksResults.SelectMany(r => r.Errors).ToList();
            logger.LogError("One or more file parts failed to download. MessageIds: {MessageIdsString}. Errors: {ErrorMessages}",
                string.Join(", ", messageIds),
                string.Join("; ", aggregatedErrors.Select(e => e.Message)));
            return Result.Fail<FileModel>(new InternalServerError("Failed to retrieve one or more file parts.")).WithErrors(aggregatedErrors);
        }

        var successfulParts = allTaskResults.Select(r => r.Value);

        try
        {
            var fileModel = await AssembleFileFromPartsAsync(successfulParts.SelectMany(x => x), "unknown_file");

            logger.LogInformation("FileEntry {FileName} successfully reconstructed from {MessageCount} messages.", fileModel.FileName, messageIds.Count);
            return Result.Ok(fileModel);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to assemble file from parts. MessageIds: {MessageIdsString}", string.Join(", ", messageIds));
            return Result.Fail<FileModel>(new InternalServerError($"Failed to assemble file: {ex.Message}").CausedBy(ex));
        }
    }

    public async Task<Result> DeleteFileAsync(List<ulong> messageIds)
    {
        var isPartiallyDeleted = false;

        logger.LogInformation("Attempting to delete file parts from {MessageCount} messages.", messageIds.Count);

        if (messageIds.Count == 0)
        {
            logger.LogWarning("No message IDs provided for file deletion.");
            return Result.Fail(new NotFoundError("File not found").WithMetadata("PartiallyDeleted", isPartiallyDeleted));
        }

        foreach (var messageId in messageIds)
        {
            try
            {
                logger.LogDebug("Deleting message {MessageId}", messageId);
                await restClient.DeleteMessageAsync(ChannelId, messageId);
                logger.LogDebug("Successfully deleted message {MessageId}", messageId);

                isPartiallyDeleted = true;
            }
            catch (RestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogWarning(ex, "Message {MessageId} not found during deletion, possibly already deleted.", messageId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete message {MessageId}.", messageId);
                return Result.Fail(new Error("Failed to delete message")
                    .CausedBy(ex)
                    .WithMetadata("PartiallyDeleted", isPartiallyDeleted));
            }
        }

        logger.LogInformation("Successfully deleted file parts from {MessageCount} messages.", messageIds.Count);
        return Result.Ok();
    }
}