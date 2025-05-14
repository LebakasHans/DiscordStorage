using DiscoDB;
using DiscoDB.Models;
using DiscoWeb.Dtos;
using DiscoWeb.Errors;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace DiscoWeb.Services;

public class FolderStorageService(DiscoContext db, IFileStorageService fileStorageService) : IFolderStorageService
{
    public async Task<Result<FolderStructureDto>> GetFolderStructureAsync(Guid? folderId)
    {
        Folder? targetFolder;
        if (folderId.HasValue)
        {
            targetFolder = await db.Folders
                .Include(f => f.Files)
                .Include(f => f.ChildFolders)
                .FirstOrDefaultAsync(f => f.Id == folderId.Value);
            if (targetFolder is null)
            {
                return Result.Fail(new NotFoundError("Specified folder does not exist"));
            }
        }
        else
        {
            targetFolder = await db.Folders
                .Include(f => f.Files)
                .Include(f => f.ChildFolders)
                .FirstOrDefaultAsync(f => f.ParentFolderId == null);
            if (targetFolder is null)
            {
                return Result.Fail(new InternalServerError("Root folder not found. Database might be uninitialized."));
            }
        }

        var currentFolderPath = await GetPathSegmentsAsync(targetFolder.Id);

        var fileEntries = targetFolder.Files.Select(f =>
        {
            var extension = Path.GetExtension(f.Name);
            var type = string.IsNullOrEmpty(extension) ? "File" : extension[1..].ToUpper() + "-File";

            return new ExplorerEntry
            {
                Id = f.Id,
                Name = f.Name,
                Size = f.Size,
                IsFile = true,
                Type = type,
                Corrupted = f.Corrupted,
                CreatedAt = f.CreatedAt,
                ModifiedAt = f.ModifiedAt
            };
        }).ToList();


        var folderEntries = targetFolder.ChildFolders.Select(f => new ExplorerEntry
        {
            Id = f.Id,
            Name = f.Name,
            IsFile = false,
            Type = "Folder",
            CreatedAt = f.CreatedAt,
            ModifiedAt = f.ModifiedAt
        }).ToList();

        var entries = fileEntries.Concat(folderEntries).ToList();

        var folderStructure = new FolderStructureDto
        {
            FolderId = targetFolder.Id,
            Name = targetFolder.Name,
            ParentFolderId = targetFolder.ParentFolderId,
            Path = currentFolderPath,
            Entries = entries,
        };

        return Result.Ok(folderStructure);
    }

    public async Task<Result<string>> CreateFolderAsync(CreateFolderDto createFolderDto)
    {
        var parentFolder = db.Folders.Include(x => x.ChildFolders).FirstOrDefault(x => x.Id == createFolderDto.ParentId);
        if (parentFolder is null)
        {
            return Result.Fail(new NotFoundError("Parent folder does not exist"));
        }

        var folderExists = parentFolder.ChildFolders.Any(x => x.Name == createFolderDto.Name);
        if (folderExists)
        {
            return Result.Fail(new ValidationError("Folder already exists"));
        }

        db.Folders.Add(new Folder
        {
            Name = createFolderDto.Name,
            ParentFolder = parentFolder,
        });
        await db.SaveChangesAsync();

        return Result.Ok("Folder created successfully");
    }

    public async Task<Result<string>> DeleteFolderAsync(Guid folderId, bool recursive, bool hardDelete)
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
                var deleteResult = await DeleteFolderAsync(childFolder.Id, true, hardDelete);
                if (deleteResult.IsFailed)
                {
                    return deleteResult;
                }
            }

            foreach (var file in folder.Files.ToList())
            {
                var deleteFileResult = await fileStorageService.DeleteFileAsync(file.Id, hardDelete);
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

    private async Task<List<SimpleFolderDto>> GetPathSegmentsAsync(Guid folderId)
    {
        List<SimpleFolderDto> pathSegments = [];
        Guid? currentFolderId = folderId;

        while (currentFolderId.HasValue)
        {
            var folder = await db.Folders
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(f => f.Id == currentFolderId.Value);

            if (folder == null) break;


            pathSegments.Add(new SimpleFolderDto
            {
                FolderId = folder.Id,
                Name = folder.Name
            });

            currentFolderId = folder.ParentFolderId;
        }

        pathSegments.Reverse();

        return pathSegments;
    }
}
