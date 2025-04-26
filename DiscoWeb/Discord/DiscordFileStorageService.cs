using DiscoWeb.Errors;
using DiscoWeb.Models; // Added using for FileModel
using FluentResults;
using NetCord.Rest;
using System.IO; // Added for MemoryStream
using System.Net.Http; // Added for HttpClient
using static System.Text.RegularExpressions.Regex; // Added for Regex

namespace DiscoWeb.Discord;

public class DiscordFileStorage(RestClient restClient, ILogger<DiscordFileStorage> logger, HttpClient httpClient) : IDiscordFileStorage
{
    private const ulong ChannelId = 1313525215837294644;
    private const long MaxChunkSize = 8 * 1024 * 1024;

    public async Task<Result<List<ulong>>> WriteFileAsync(IFormFile file)
    {
        logger.LogInformation("Attempting to upload file {FileName} of size {FileSize} bytes.", file.FileName, file.Length);
        List<ulong> messageIds = [];

        try
        {
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
                    var messageProperties = new MessageProperties().WithAttachments([attachment]);

                    var sentMessage = await restClient.SendMessageAsync(ChannelId, messageProperties);
                    messageIds.Add(sentMessage.Id);

                    partNumber++;

                }
            }

            logger.LogInformation("FileEntry {FileName} uploaded successfully in {MessageCount} messages.", file.FileName, messageIds.Count);
            return Result.Ok(messageIds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to store file {FileName}.", file.FileName);
            return Result.Fail(new Error($"Failed to store file: {ex.Message}")
                .CausedBy(ex));
        }
    }

    public async Task<Result<FileModel>> ReadFileAsync(List<ulong> messageIds)
    {
        logger.LogInformation("Attempting to read file from {MessageCount} messages.", messageIds.Count);
        var combinedStream = new MemoryStream();
        string originalFileName = string.Empty;

        try
        {
            if (messageIds == null || messageIds.Count == 0)
            {
                logger.LogWarning("No message IDs provided for reading file.");
                return Result.Fail(new ValidationError("No message IDs provided"));
            }

            for (int i = 0; i < messageIds.Count; i++)
            {
                var messageId = messageIds[i];
                logger.LogDebug("Fetching message {MessageId} ({Index}/{Total}).", messageId, i + 1, messageIds.Count);
                var message = await restClient.GetMessageAsync(ChannelId, messageId);

                if (message.Attachments.Count != 1)
                {
                    logger.LogWarning("Message {MessageId} has {AttachmentCount} attachments, expected 1.", messageId, message.Attachments.Count);

                    if (message.Attachments.Count == 0)
                    {
                        await combinedStream.DisposeAsync();
                        return Result.Fail(new InternalServerError($"Message {messageId} contains no attachments."));
                    }

                    await combinedStream.DisposeAsync();
                    return Result.Fail(new InternalServerError($"Message {messageId} contains {message.Attachments.Count} attachments, expected exactly 1."));
                }

                var attachment = message.Attachments[0];
                logger.LogDebug("Downloading attachment {AttachmentFileName} from URL {AttachmentUrl}", attachment.FileName, attachment.Url);

                if (i == 0)
                {
                    originalFileName = Replace(attachment.FileName, @"\.part\d+$", "");
                    if (string.IsNullOrEmpty(originalFileName))
                    {
                        logger.LogWarning("Could not extract original filename from {AttachmentFileName}. Using attachment name as fallback.", attachment.FileName);
                        originalFileName = attachment.FileName;
                    }
                }

                using var response = await httpClient.GetAsync(attachment.Url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                await using var attachmentStream = await response.Content.ReadAsStreamAsync();
                await attachmentStream.CopyToAsync(combinedStream);
                logger.LogDebug("Finished downloading and appending part {Index}/{Total}.", i, messageIds.Count);
            }

            combinedStream.Position = 0;
            var fileBytes = combinedStream.ToArray(); // Read stream into byte array
            await combinedStream.DisposeAsync(); // Dispose the stream after reading

            logger.LogInformation("FileEntry {FileName} successfully reconstructed from {MessageCount} messages.", originalFileName, messageIds.Count);

            var fileModel = new FileModel
            {
                FileName = originalFileName,
                Content = fileBytes
            };

            return Result.Ok(fileModel); // Return the FileModel
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read file from {MessageCount} messages.", messageIds.Count);
            await combinedStream.DisposeAsync();
            return Result.Fail(new InternalServerError($"Failed to read file: {ex.Message}").CausedBy(ex));
        }
    }

    public Task<Result> DeleteFileAsync(List<ulong> messageIds)
    {
        throw new NotImplementedException();
    }
}