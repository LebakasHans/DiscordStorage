using DiscoWeb.Dtos;
using FluentResults;

namespace DiscoWeb.Services;

public interface IFolderStorageService
{
    Task<Result<FolderStructureDto>> GetFolderStructureAsync(Guid? folderId);
    Task<Result<string>> CreateFolderAsync(CreateFolderDto createFolderDto);
    Task<Result<string>> DeleteFolderAsync(Guid folderId, bool recursive, bool hardDelete);
}