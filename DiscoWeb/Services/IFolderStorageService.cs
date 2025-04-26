using DiscoWeb.Dtos;
using FluentResults;

namespace DiscoWeb.Services;

public interface IFolderStorageService
{
    Task<Result<FolderStructureDto>> GetFolderStructureAsync(Guid folderId, int depth);
    Task<Result<string>> CreateFolderAsync(FolderDto folderDto);
    Task<Result<string>> DeleteFolderAsync(Guid folderId);
}