# SAESRPredictionBot

A Discord bot to create / lock / cancel and resolve Twitch chat predictions

# Getting started

- Build the bot using the `dotnet build` tool
- Create a `discord-secrets` file in the executable's directory
- Create a `twitch-secrets` file in the executable's directory

discord-secrets file schema:
```json
{
  "token": "Your bot token",
  "lowestAllowedRoleId": 1234567890123456789
}
```

The `lowestAllowedRoleId` represents the lowest role in the hierarchy that is able to execute the Slash Commands

twitch-secrets file schema:
```json
{
  "clientId": "Your client id",
  "accessToken": "Your OAuth Token",
  "secret": "Your client secret",
  "broadcasterId": "123456789"
}
```