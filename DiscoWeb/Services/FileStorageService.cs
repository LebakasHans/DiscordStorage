using DiscoDB;
using DiscoDB.Models;
using DiscoWeb.Discord;
using DiscoWeb.Errors;
using DiscoWeb.Models;
using FluentResults;

namespace DiscoWeb.Services;

public class FileStorageService(DiscoContext db, IDiscordFileStorage discordFileStorage) : IFileStorageService
{
    public async Task<Result<FileModel>> GetFileAsync(Guid fileId)
    {
        var file = db.Files.FirstOrDefault(x => x.Id == fileId);
        if (file is null)
        {
            return Result.Fail(new NotFoundError("File does not exist"));
        }

        var result = await discordFileStorage.ReadFileAsync(file.MessageIds);

        return result;
    }

    public async Task<Result<string>> DeleteFileAsync(Guid fileId)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<string>> UploadFileAsync(Guid folderId, IFormFile file)
    {
        bool fileExists = db.Files.Any(x => x.FolderId == folderId && x.Name == file.FileName);
        if (fileExists)
        {
            return Result.Fail(new Error("File with this name already exists in the target folder"));
        }

        var folder = await db.Folders.FindAsync(folderId);
        if (folder is null)
        {
            return Result.Fail(new NotFoundError("Target folder does not exist"));
        }

        var result = await discordFileStorage.WriteFileAsync(file);
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        var messageIds = result.Value;

        db.Files.Add(new FileEntry
        {
            Folder = folder,
            Name = file.FileName,
            MessageIds = messageIds,
            Size = file.Length
        });

        await db.SaveChangesAsync();

        return Result.Ok("FileEntry uploaded successfully");
    }
}
