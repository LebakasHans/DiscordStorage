using DiscoWeb.Dtos;
using DiscoWeb.Models;
using FluentResults;

namespace DiscoWeb.Services;

public interface IFileStorageService
{
    Task<Result<FileModel>> GetFileAsync(Guid fileId);
    Task<Result<string>> DeleteFileAsync(Guid fileId, bool hardDelete);
    Task<Result<string>> UploadFileAsync(Guid folderId, IFormFile file);
}