using DiscoDB;
using DiscoDB.Models;
using DiscoWeb.Discord;
using DiscoWeb.Dtos;
using DiscoWeb.ResultConversion;
using DiscoWeb.Services;
using FluentResults.Extensions.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NetCord;
using NetCord.Rest;
using System.Text;
using System.Text.Json;

AspNetCoreResult.Setup(config => config.DefaultProfile = new CustomFluentResultsProfile());

const string myAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(myAllowSpecificOrigins,
        b =>
        {
            b.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

builder.Services.AddDbContext<DiscoContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
    );

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();

#region JWT
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    _ = builder.Configuration["JWT:PasswordHash"]
        ?? throw new InvalidOperationException(
            "JWT Password not found in configuration. Ensure 'JWT:Password is set in your appsettings.json or environment variables.");

    var secret = builder.Configuration["JWT:Secret"]
                 ?? throw new InvalidOperationException(
                     "JWT secret not found in configuration. Ensure 'JWT:Secret is set in your appsettings.json or environment variables.");

    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ValidateIssuer = false,
        ValidateAudience = false,
    };

    x.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new SimpleResponse
            {
                Status = "401",
                Message = "Unauthorized: Access token is missing, invalid, or expired."
            };

            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, serializerOptions));
        }
    };
});
#endregion

#region Service Registration
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
#endregion

#region Configure File Uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue;
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = null;
});
#endregion

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

app.UseCors(myAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
