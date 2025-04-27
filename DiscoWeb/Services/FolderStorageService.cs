using DiscoDB;
using DiscoDB.Models;
using DiscoWeb.Dtos;
using DiscoWeb.Errors;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace DiscoWeb.Services;

public class FolderStorageService(DiscoContext db, IFileStorageService fileStorageService) : IFolderStorageService
{
    public async Task<Result<FolderStructureDto>> GetFolderStructureAsync(Guid? folderId, int depth)
    {
        Folder? startFolder;
        if (folderId.HasValue)
        {
            startFolder = await db.Folders
                .Include(f => f.Files)
                .FirstOrDefaultAsync(f => f.Id == folderId.Value);
            if (startFolder is null)
            {
                return Result.Fail(new NotFoundError("Specified folder does not exist"));
            }
        }
        else
        {
            startFolder = await db.Folders
                .Include(f => f.Files)
                .FirstOrDefaultAsync(f => f.ParentFolderId == null);
            if (startFolder is null)
            {
                return Result.Fail(new Error("Root folder not found. Database might be uninitialized."));
            }
        }

        var folderStructure = await BuildFolderStructureRecursiveAsync(startFolder, depth);
        return Result.Ok(folderStructure);
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

    public async Task<Result<string>> DeleteFolderAsync(Guid folderId, bool recursive)
    {
        var folder = await db.Folders
            .Include(x => x.ChildFolders)
            .Include(x => x.Files)
            .FirstOrDefaultAsync(x => x.Id == folderId);

        if (folder is null)
            return Result.Fail(new NotFoundError("Folder does not exist"));

        if (folder.ParentFolderId == null)
            return Result.Fail(new ValidationError("Cannot delete the root folder"));

        if (!recursive && (folder.ChildFolders.Count != 0 || folder.Files.Count != 0))
            return Result.Fail(new ValidationError("Folder is not empty"));


        if (recursive)
        {
            foreach (var childFolder in folder.ChildFolders.ToList())
            {
                var deleteResult = await DeleteFolderAsync(childFolder.Id, true);
                if (deleteResult.IsFailed)
                {
                    return deleteResult;
                }
            }

            foreach (var file in folder.Files.ToList())
            {
                var deleteFileResult = await fileStorageService.DeleteFileAsync(file.Id);
                if (deleteFileResult.IsFailed)
                {
                    return deleteFileResult;
                }
            }
        }

        db.Folders.Remove(folder);
        await db.SaveChangesAsync();

        return Result.Ok("Folder deleted successfully");
    }

    private async Task<FolderStructureDto> BuildFolderStructureRecursiveAsync(Folder folder, int depth)
    {
        var dto = new FolderStructureDto
        {
            FolderId = folder.Id,
            Name = folder.Name,
            Files = folder.Files.Select(f => new FileDto
            {
                Id = f.Id,
                Name = f.Name,
                Size = f.Size,
                Corrupted = f.Corrupted,
                CreatedAt = f.CreatedAt,
                ModifiedAt = f.ModifiedAt
            }).ToList()
        };

        if (depth > 0)
        {
            await db.Entry(folder).Collection(f => f.ChildFolders).LoadAsync();
            foreach (var subFolder in folder.ChildFolders)
            {
                await db.Entry(subFolder).Collection(f => f.Files).LoadAsync();
                dto.SubFolders.Add(await BuildFolderStructureRecursiveAsync(subFolder, depth - 1));
            }
        }

        return dto;
    }
}
