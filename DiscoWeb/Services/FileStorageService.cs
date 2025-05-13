using DiscoDB;
using DiscoDB.Models;
using DiscoWeb.Discord;
using DiscoWeb.Errors;
using DiscoWeb.Models;
using FluentResults;

namespace DiscoWeb.Services;

public class FileStorageService(DiscoContext db, IDiscordFileStorage discordFileStorage, ILogger<FileStorageService> logger) : IFileStorageService
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
        var file = await db.Files.FindAsync(fileId);
        if (file is null)
        {
            return Result.Fail(new NotFoundError("File does not exist"));
        }

        //var result = await discordFileStorage.DeleteFileAsync(file.MessageIds);

        //if (result.IsFailed)
        //{
        //    if (bool.TryParse(result.Errors[0].Metadata["PartiallyDeleted"].ToString(), out var isPartiallyDeleted))
        //    {
        //        file.Corrupted = isPartiallyDeleted;
        //        await db.SaveChangesAsync();

        //        return Result.Fail(isPartiallyDeleted 
        //            ? new Error("An error occurred while deleting the file. The file is now marked as corrupted. Please try again.") 
        //            : new Error("An error occurred while deleting the file. The file remains intact and should still be accessible."));
        //    }

        //    logger.LogError("Failed to delete file {FileName}.", file.Name);
        //    return Result.Fail(new InternalServerError("Something went wrong while Deleting the file. It might be corrupted."));
        //}

        var removedFile = await db.Files.FindAsync(fileId);
        if (removedFile != null)
        {
            db.Files.Remove(removedFile);
            await db.SaveChangesAsync();
        }
        else
        {
            logger.LogWarning("File removed successfully but not found in Database");
        }

        return Result.Ok("File deleted successfully");
    }

    public async Task<Result<string>> UploadFileAsync(Guid folderId, IFormFile file)
    {
        bool fileExists = db.Files.Any(x => x.FolderId == folderId && x.Name == file.FileName);
        if (fileExists)
        {
            return Result.Fail(new ValidationError("File with this name already exists in the target folder"));
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
