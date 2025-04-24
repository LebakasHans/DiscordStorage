using FluentResults;
using NetCord.Rest;

namespace DiscoWeb.Helper.Discord;

public class FileStorageHelper
{
    public static async Task<Result> WriteFile(IFormFile file, RestClient restClient)
    {
        const ulong channelId = 1313525215837294644;
        
        string fileContent;

        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            fileContent = await reader.ReadToEndAsync();
        }
        
        var test = await restClient.SendMessageAsync(channelId, fileContent);
        return Result.Ok();
    }
}