using DiscoWeb.Dtos;
using FluentResults;

namespace DiscoWeb.Services;

public interface IStorageService
{
    Task<Result<FolderStructureDto>> GetFolderStructureAsync(string path, int depth);
    Task<Result<string>> CreateFolderAsync(string path);
    Task<Result<string>> DeleteFolderAsync(string path);
    Task<Result<string>> GetFileAsync(string path);
    Task<Result<string>> DeleteFileAsync(string path);
    Task<Result<string>> UploadFileAsync(string path, IFormFile file);
}