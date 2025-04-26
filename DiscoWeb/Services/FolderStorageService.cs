using DiscoDB;
using DiscoDB.Models;
using DiscoWeb.Dtos;
using DiscoWeb.Errors;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace DiscoWeb.Services;

public class FolderStorageService(DiscoContext db) : IFolderStorageService
{
    public async Task<Result<FolderStructureDto>> GetFolderStructureAsync(Guid folderId, int depth)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<string>> CreateFolderAsync(FolderDto folderDto)
    {
        var parentFolder = db.Folders.Include(x => x.ChildFolders).FirstOrDefault(x => x.Id == folderDto.ParentId);
        if (parentFolder is null)
        {
            return Result.Fail(new NotFoundError("Parent folder does not exist"));
        }

        var folderExists = parentFolder.ChildFolders.Any(x => x.Name == folderDto.Name);
        if (folderExists)
        {
            return Result.Fail(new Error("Folder already exists"));
        }

        db.Folders.Add(new Folder
        {
            Name = folderDto.Name,
            ParentFolder = parentFolder,
        });
        await db.SaveChangesAsync();

        return Result.Ok("Folder created successfully");
    }

    public async Task<Result<string>> DeleteFolderAsync(Guid folderId)
    {
        var folder = await db.Folders.Include(x => x.ChildFolders).FirstOrDefaultAsync(x => x.Id == folderId);
        if (folder is null)
        {
            return Result.Fail(new NotFoundError("Folder does not exist"));
        }

        if (folder.ChildFolders.Count != 0)
        {
            return Result.Fail(new Error("Folder is not empty"));
        }

        db.Folders.Remove(folder);
        await db.SaveChangesAsync();

        return Result.Ok("Folder deleted successfully");
    }
}
