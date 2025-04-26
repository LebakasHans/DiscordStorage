using DiscoWeb.BackgroundServices;
using DiscoWeb.Models;
using DiscoWeb.Queues;
using DiscoWeb.ResultConversion;
using DiscoWeb.Services;
using FluentResults.Extensions.AspNetCore;
using NetCord;
using NetCord.Rest;

AspNetCoreResult.Setup(config => config.DefaultProfile = new CustomFluentResultsProfile());

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddSingleton<ITaskQueue<StorageTask>, StorageTaskQueue>();
builder.Services.AddSingleton<DiscoWeb.Services.Processors.FileTaskProcessor>();
builder.Services.AddSingleton<DiscoWeb.Services.Processors.FolderTaskProcessor>();

builder.Services.AddSingleton(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var botTokenString = configuration["Discord:BotToken"]
        ?? throw new InvalidOperationException("Discord bot token not found in configuration. Ensure 'Discord:BotToken' is set in your appsettings.json or environment variables.");

    return new RestClient(new BotToken(botTokenString));
});

builder.Services.AddHostedService<DiscordBotWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
