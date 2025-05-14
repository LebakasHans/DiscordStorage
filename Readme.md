## Setup

```bash
cd DiscordWeb
dotnet restore
dotnet user-secrets init
dotnet user-secrets set "Discord:BotToken" "your-token"
dotnet user-secrets set "JWT:Secret" "your-secret"
dotnet user-secrets set "JWT:Password" "your-password"
dotnet run
