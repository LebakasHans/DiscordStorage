using DiscoWeb.Services;
using FluentResults;
using FluentResults.Extensions.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace DiscoWeb.Controllers;

[Route("file")]
[ApiController]
public class FileController(IStorageService storageService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetFile([FromQuery] string path)
    {
        var result = await storageService.GetFileAsync(path);

        if (result.IsFailed)
        {
            return result.ToActionResult();
        }

        // For successful results, return a file download
        string fileName = Path.GetFileName(path);
        byte[] fileBytes = Convert.FromBase64String(result.Value);
        
        return Result.Ok(File(fileBytes, "application/octet-stream", fileName))
            .ToActionResult();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteFile([FromQuery] string path)
    {
        var result = await storageService.DeleteFileAsync(path);

        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> UploadFile([FromQuery] string path, IFormFile file)
    {
        var result = await storageService.UploadFileAsync(path, file);

        return result.ToActionResult();
    }
}