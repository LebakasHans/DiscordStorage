using DiscoWeb.Dtos;
using DiscoWeb.Services;
using FluentResults.Extensions.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscoWeb.Controllers;

[Authorize]
[Route("folder")]
[ApiController]
public class FolderController(IFolderStorageService fileSystemService) : ControllerBase
{
    [HttpGet("{folderId:guid}/structure")]
    public async Task<IActionResult> GetFolderStructure(Guid folderId)
    {
        var result = await fileSystemService.GetFolderStructureAsync(folderId);

        return result.ToActionResult();
    }

    [HttpGet("root/structure")]
    public async Task<IActionResult> GetRootFolderStructure()
    {
        var result = await fileSystemService.GetFolderStructureAsync(null);

        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> CreateFolder([FromBody] CreateFolderDto createFolderDto)
    {
        var result = await fileSystemService.CreateFolderAsync(createFolderDto);

        return result.ToActionResult();
    }

    [HttpDelete("{folderId:guid}")]
    public async Task<IActionResult> DeleteFolder(Guid folderId, [FromQuery] bool recursive = false, [FromQuery] bool hardDelete = false)
    {
        var result = await fileSystemService.DeleteFolderAsync(folderId, recursive, hardDelete);

        return result.ToActionResult();
    }
}