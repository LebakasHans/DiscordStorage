using DiscoWeb.Dtos;
using DiscoWeb.Services;
using FluentResults.Extensions.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace DiscoWeb.Controllers;

[Route("folder")]
[ApiController]
public class FolderController(IFolderStorageService fileSystemService) : ControllerBase
{
    [HttpGet("structure/{folderId:guid}")]
    public async Task<IActionResult> GetFolderStructure(Guid folderId, [FromQuery]int depth = 1)
    {
        var result = await fileSystemService.GetFolderStructureAsync(folderId, depth);

        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> CreateFolder([FromBody] FolderDto folderDto)
    {
        var result = await fileSystemService.CreateFolderAsync(folderDto);

        return result.ToActionResult();
    }

    [HttpDelete("{folderId:guid}")]
    public async Task<IActionResult> DeleteFolder(Guid folderId)
    {
        var result = await fileSystemService.DeleteFolderAsync(folderId);

        return result.ToActionResult();
    }
}