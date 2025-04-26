using DiscoWeb.Services;
using FluentResults.Extensions.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace DiscoWeb.Controllers;

[Route("file")]
[ApiController]
public class FileController(IFileStorageService storageService) : ControllerBase
{
    [HttpGet("{fileId:guid}")]
    public async Task<IActionResult> GetFile(Guid fileId)
    {
        var result = await storageService.GetFileAsync(fileId);

        if (result.IsFailed)
        {
            return result.ToActionResult();
        }

        //cannot use result here as .ToActionResult does not work properly for File download
        return File(result.Value.Content, "application/octet-stream", result.Value.FileName);
    }

    [HttpDelete("{fileId:guid}")]
    public async Task<IActionResult> DeleteFile(Guid fileId)
    {
        var result = await storageService.DeleteFileAsync(fileId);

        return result.ToActionResult();
    }

    [HttpPost("{folderId:guid}")]
    public async Task<IActionResult> UploadFile(Guid folderId, IFormFile file)
    {
        var result = await storageService.UploadFileAsync(folderId, file);

        return result.ToActionResult();
    }
}
