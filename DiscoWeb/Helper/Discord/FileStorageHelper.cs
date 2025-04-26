using System.Text;
using DiscoWeb.Models;
using FluentResults;
using NetCord.Rest;

namespace DiscoWeb.Helper.Discord;

public class FileStorageHelper
{
    private const int MaxMessageLength = 1900;
    private const ulong ChannelId = 1313525215837294644;

    public static async Task<Result<List<ulong>>> WriteFile(IFormFile file, RestClient restClient)
    {
        try
        {
            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            string base64Content = Convert.ToBase64String(fileBytes);
            
            var messageIds = new List<ulong>();
            
            for (int i = 0; i < base64Content.Length; i += MaxMessageLength)
            {
                int chunkSize = Math.Min(MaxMessageLength, base64Content.Length - i);
                string chunk = base64Content.Substring(i, chunkSize);
                
                //Format: CHUNK:{chunkIndex}:{totalChunks}:{fileName}:{chunkData}
                string chunkMessage = $"CHUNK:{i/MaxMessageLength}:{Math.Ceiling((double)base64Content.Length/MaxMessageLength)}:{file.FileName}:{chunk}";
                
                var message = await restClient.SendMessageAsync(ChannelId, chunkMessage);
                messageIds.Add(message.Id);
            }

            return Result.Ok(messageIds);
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error($"Failed to store file: {ex.Message}")
                .CausedBy(ex));
        }
    }
    
    public static async Task<Result<FileModel>> ReadFile(List<ulong> messageIds, RestClient restClient)
    {
        try
        {
            if (messageIds.Count == 0)
            {
                return Result.Fail("No message IDs provided");
            }

            var chunks = new List<(int index, string content, string fileName)>();
            string fileName = null;
            int totalChunks = 0;

            // Fetch all messages and parse chunk information
            foreach (var messageId in messageIds)
            {
                var message = await restClient.GetMessageAsync(ChannelId, messageId);
                
                if (string.IsNullOrEmpty(message.Content))
                {
                    return Result.Fail($"Message with ID {messageId} not found or has no content");
                }

                if (!message.Content.StartsWith("CHUNK:"))
                {
                    return Result.Fail($"Message with ID {messageId} is not a valid file chunk");
                }

                // Parse chunk metadata (format: CHUNK:index:total:filename:content)
                string[] parts = message.Content.Split(':', 5);
                if (parts.Length != 5)
                {
                    return Result.Fail($"Invalid chunk format in message {messageId}");
                }

                if (!int.TryParse(parts[1], out int chunkIndex))
                {
                    return Result.Fail($"Invalid chunk index in message {messageId}");
                }

                if (!double.TryParse(parts[2], out double total))
                {
                    return Result.Fail($"Invalid total chunks count in message {messageId}");
                }

                totalChunks = (int)total;
                string chunkFileName = parts[3];
                string content = parts[4];

                // Verify all chunks have the same filename
                if (fileName == null)
                {
                    fileName = chunkFileName;
                }
                else if (fileName != chunkFileName)
                {
                    return Result.Fail("Inconsistent filenames across chunks");
                }

                chunks.Add((chunkIndex, content, chunkFileName));
            }

            // Verify we have all chunks
            if (chunks.Count != totalChunks)
            {
                return Result.Fail($"Missing chunks: Expected {totalChunks}, got {chunks.Count}");
            }

            // Reassemble file content in correct order
            chunks.Sort((a, b) => a.index.CompareTo(b.index));
            
            var base64Content = new StringBuilder();
            foreach (var chunk in chunks)
            {
                base64Content.Append(chunk.content);
            }

            // Convert base64 back to bytes
            byte[] fileBytes = Convert.FromBase64String(base64Content.ToString());

            return Result.Ok(new FileModel
            {
                FileName = fileName!,
                Content = fileBytes
            });
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error($"Failed to read file: {ex.Message}")
                .CausedBy(ex));
        }
    }
}