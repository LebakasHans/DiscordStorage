using DiscoWeb.Services;
using FluentResults.Extensions.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace DiscoWeb.Controllers;

[Route("file")]
[ApiController]
public class FileController(IStorageService storageService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetFile([FromQuery] string path)
    {
        var result = await storageService.GetFileAsync(path);

        return result.ToActionResult();
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