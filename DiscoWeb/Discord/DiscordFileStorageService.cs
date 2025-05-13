using DiscoWeb.Errors;
using DiscoWeb.Models;
using FluentResults;
using NetCord.Rest;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DiscoWeb.Discord;

public class DiscordFileStorage(RestClient restClient, ILogger<DiscordFileStorage> logger, HttpClient httpClient) : IDiscordFileStorage
{
    private const ulong ChannelId = 1313525215837294644;
    private const long MaxChunkSize = 8 * 1024 * 1024;

    public async Task<Result<List<ulong>>> WriteFileAsync(IFormFile file)
    {
        logger.LogInformation("Attempting to upload file {FileName} of size {FileSize} bytes.", file.FileName, file.Length);
        var stopwatch = Stopwatch.StartNew();
        List<ulong> messageIds = new();

        try
        {
            await using var stream = file.OpenReadStream();
            var buffer = new byte[MaxChunkSize];
            int partNumber = 1;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
            using var chunkStream = new MemoryStream(buffer, 0, bytesRead);
            var partFileName = $"{file.FileName}.part{partNumber}";

            var attachment = new AttachmentProperties(partFileName, chunkStream);
            var messageProperties = new MessageProperties().WithAttachments(new[] { attachment });

            var sentMessage = await restClient.SendMessageAsync(ChannelId, messageProperties);
            messageIds.Add(sentMessage.Id);

            partNumber++;
            }

            stopwatch.Stop();
            logger.LogWarning("File {FileName} uploaded in {ElapsedMilliseconds} ms.", file.FileName, stopwatch.ElapsedMilliseconds);
            logger.LogInformation("FileEntry {FileName} uploaded successfully in {MessageCount} messages. Time taken: {ElapsedMilliseconds} ms.", 
            file.FileName, messageIds.Count, stopwatch.ElapsedMilliseconds);
            return Result.Ok(messageIds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Failed to store file {FileName}. Time taken: {ElapsedMilliseconds} ms.", file.FileName, stopwatch.ElapsedMilliseconds);
            return Result.Fail(new Error($"Failed to store file: {ex.Message}")
            .CausedBy(ex));
        }
    }

    private async Task<Result<FilePart>> DownloadFilePartAsync(ulong messageId, int index, int totalCount)
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
                return Result.Fail<FilePart>(new InternalServerError($"Failed to fetch message details for {messageId}. Discord API error: {ex.Message}").CausedBy(ex));
            }

            if (message.Attachments.Count == 0)
            {
                var errorMsg = $"Message {messageId} contains no attachments.";
                logger.LogWarning(errorMsg);
                return Result.Fail<FilePart>(new InternalServerError(errorMsg));
            }
            if (message.Attachments.Count != 1)
            {
                var errorMsg = $"Message {messageId} contains {message.Attachments.Count} attachments, expected exactly 1.";
                logger.LogWarning(errorMsg);
                return Result.Fail<FilePart>(new InternalServerError(errorMsg));
            }

            var attachment = message.Attachments[0];
            logger.LogDebug("Downloading attachment {AttachmentFileName} from URL {AttachmentUrl} for message {MessageId}.", attachment.FileName, attachment.Url, messageId);

            using var response = await httpClient.GetAsync(attachment.Url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = $"Failed to download attachment {attachment.FileName}. HTTP status code: {response.StatusCode}";
                logger.LogWarning(errorMsg);
                return Result.Fail<FilePart>(new InternalServerError(errorMsg));
            }

            var contentBytes = await response.Content.ReadAsByteArrayAsync();

            logger.LogDebug("Finished downloading part {IndexPlusOne}/{Total} for message {MessageId}.", index + 1, totalCount, messageId);
            return Result.Ok(new FilePart
            {
                Index = index,
                Content = contentBytes,
                RawFileName = attachment.FileName
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error processing message {MessageId} for part {IndexPlusOne}.", messageId, index + 1);
            return Result.Fail<FilePart>(new InternalServerError($"Unexpected error processing part for message {messageId}.").CausedBy(ex));
        }
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
        if (failedTasksResults.Any())
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
            var fileModel = await AssembleFileFromPartsAsync(successfulParts, "unknown_file"); 
            
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