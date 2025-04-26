using DiscoDB;
using DiscoDB.Models;
using DiscoWeb.Discord;
using DiscoWeb.ResultConversion;
using DiscoWeb.Services;
using FluentResults.Extensions.AspNetCore;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Rest;

AspNetCoreResult.Setup(config => config.DefaultProfile = new CustomFluentResultsProfile());

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DiscoContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();

builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IFolderStorageService, FolderStorageService>();

builder.Services.AddSingleton<IDiscordFileStorage, DiscordFileStorage>();
builder.Services.AddSingleton(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var botTokenString = configuration["Discord:BotToken"]
        ?? throw new InvalidOperationException("Discord bot token not found in configuration. Ensure 'Discord:BotToken' is set in your appsettings.json or environment variables.");

    return new RestClient(new BotToken(botTokenString));
});

var app = builder.Build();

#region Database Initialization
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DiscoContext>();

    if (db.Database.EnsureCreated())
    {
        db.Folders.Add(new Folder
        {
            Name = "",
        });
        db.SaveChanges();
    }
}
#endregion

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
