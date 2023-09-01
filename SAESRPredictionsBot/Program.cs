using DSharpPlus;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TwitchLib.Api;
using TwitchLib.Api.Core;
using TwitchLib.Api.Core.Enums;

namespace SAESRPredictionsBot;

internal class Program
{
    private static string ProgramDirectory = AppDomain.CurrentDomain.BaseDirectory;
    
    public static async Task Main(string[] args)
    {
        string discordSecretsjson = File.ReadAllText(Path.Combine(ProgramDirectory, "discord-secrets"));
        string twitchSecretsJson = File.ReadAllText(Path.Combine(ProgramDirectory, "twitch-secrets"));
        DiscordSecrets discordSecrets = JsonConvert.DeserializeObject<DiscordSecrets>(discordSecretsjson)!;
        TwitchSecrets twitchSecrets = JsonConvert.DeserializeObject<TwitchSecrets>(twitchSecretsJson)!;
        
        DiscordConfiguration discordConfiguration = new DiscordConfiguration()
        {
            Intents = DiscordIntents.AllUnprivileged,
            Token = discordSecrets.Token,
            TokenType = TokenType.Bot
        };

        ApiSettings twitchSettings = new ApiSettings()
        {
            ClientId = twitchSecrets.ClientId,
            AccessToken = twitchSecrets.AccessToken,
            Secret = twitchSecrets.Secret,
            Scopes = new List<AuthScopes> { AuthScopes.Helix_Channel_Read_Predictions, AuthScopes.Helix_Channel_Manage_Predictions }
        };
        
        DiscordClient discordClient = new DiscordClient(discordConfiguration);
        TwitchAPI twitchApi = new TwitchAPI(settings: twitchSettings);

        ServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(twitchApi);
        serviceCollection.AddSingleton(new BroadcasterId(twitchSecrets.BroadcasterId));
        serviceCollection.AddSingleton(discordSecrets);
        
        SlashCommandsExtension slashCommands = discordClient.UseSlashCommands(new SlashCommandsConfiguration()
        {
            Services = serviceCollection.BuildServiceProvider()
        });
        
        discordClient.GuildDownloadCompleted += async (sender, eventArgs) =>
        {
            sender.Logger.LogInformation("Guilds download complete");
        };

        discordClient.ComponentInteractionCreated += async (sender, eventArgs) =>
        {
            if (eventArgs.Interaction.Data.CustomId is CreateInteractivityContext.YesButtonId
                     or CreateInteractivityContext.CancelButtonId)
            {
                await PredictionsCommands.Instance.HandleCreateInteractivity(sender, eventArgs);
                return;
            }
            else if (eventArgs.Interaction.Data.CustomId is CancelInteractivityContext.YesButtonId
                     or CancelInteractivityContext.NoButtonId)
            {
                await PredictionsCommands.Instance.HandleCancelInteractivity(sender, eventArgs);
                return;
            }
            else if (eventArgs.Interaction.Data.CustomId is LockInteractivityContext.YesButtonId
                or LockInteractivityContext.NoButtonId)
            {
                await PredictionsCommands.Instance.HandleLockInteractivity(sender, eventArgs);
                return;
            }
            else if (eventArgs.Interaction.Data.CustomId.StartsWith(ResolveInteractivityContext.CommonButtonId))
            {
                await PredictionsCommands.Instance.HandleResolveInteractivity(sender, eventArgs);
                return;
            }
            
            sender.Logger.LogInformation($"Unknown component interaction triggered: '{eventArgs.Interaction.Data.CustomId}'");
        };
        
        slashCommands.RegisterCommands<PredictionsCommands>();
        
        await discordClient.ConnectAsync();
        await Task.Delay(-1);
    }
}