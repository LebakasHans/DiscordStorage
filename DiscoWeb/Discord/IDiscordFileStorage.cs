using DiscoWeb.Models;
using FluentResults;

namespace DiscoWeb.Discord
{
    public interface IDiscordFileStorage
    {
        Task<Result<List<ulong>>> WriteFileAsync(IFormFile file);
        Task<Result<FileModel>> ReadFileAsync(List<ulong> messageIds);
        Task<Result> DeleteFileAsync(List<ulong> messageIds);
    }
}