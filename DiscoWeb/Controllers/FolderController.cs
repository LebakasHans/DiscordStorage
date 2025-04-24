using DiscoWeb.Services;
using FluentResults.Extensions.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace DiscoWeb.Controllers;

[Route("folder")]
[ApiController]
public class FolderController(IStorageService fileSystemService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateFolder([FromQuery] string path)
    {
        var result = await fileSystemService.CreateFolderAsync(path);

        return result.ToActionResult();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteFolder([FromQuery] string path)
    {
        var result = await fileSystemService.DeleteFolderAsync(path);

        return result.ToActionResult();
    }
}