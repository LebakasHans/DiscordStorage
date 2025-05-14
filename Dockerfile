# Use a base image for ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# Create the /app/Db directory and set permissions
RUN mkdir -p /app/Db && chmod -R 777 /app/Db

# Use SDK image to build the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DiscoWeb/DiscoWeb.csproj", "DiscoWeb/"]
COPY ["DiscoDB/DiscoDB.csproj", "DiscoDB/"]
RUN dotnet restore "./DiscoWeb/DiscoWeb.csproj"
COPY . .
WORKDIR "/src/DiscoWeb"
RUN dotnet build "./DiscoWeb.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DiscoWeb.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image setup for production
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DiscoWeb.dll"]
