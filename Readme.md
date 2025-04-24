## Setup

```bash
cd DiscordWeb
dotnet restore
dotnet user-secrets init
dotnet user-secrets set "Discord:BotToken" "your-token"
dotnet run
