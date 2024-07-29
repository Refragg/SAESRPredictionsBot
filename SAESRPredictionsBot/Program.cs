using DSharpPlus;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core;
using TwitchLib.Api.Core.Enums;
using Timer = System.Timers.Timer;

namespace SAESRPredictionsBot;

internal class Program
{
    private static string _twitchRefreshToken;

    private static TwitchAPI _twitchApi;

    private static Timer _refreshTwitchTokenTimer = new (180_000) { AutoReset = true };

    private static TwitchSecrets _twitchSecrets;
    
    public static async Task Main(string[] args)
    {
        string discordSecretsjson = File.ReadAllText("/config/discord-secrets");
        DiscordSecrets discordSecrets = JsonConvert.DeserializeObject<DiscordSecrets>(discordSecretsjson)!;
        
        _twitchSecrets = TwitchSecrets.Load();
        
        DiscordConfiguration discordConfiguration = new DiscordConfiguration()
        {
            Intents = DiscordIntents.AllUnprivileged,
            Token = discordSecrets.Token,
            TokenType = TokenType.Bot
        };

        ApiSettings twitchSettings = new ApiSettings()
        {
            ClientId = _twitchSecrets.ClientId,
            Secret = _twitchSecrets.Secret,
            AccessToken = _twitchSecrets.AccessToken,
            Scopes = new List<AuthScopes> { AuthScopes.Helix_Channel_Read_Predictions, AuthScopes.Helix_Channel_Manage_Predictions }
        };
        _twitchRefreshToken = _twitchSecrets.RefreshToken;
        
        DiscordClient discordClient = new DiscordClient(discordConfiguration);
        _twitchApi = new TwitchAPI(settings: twitchSettings);

        await RefreshTwitchToken();
        
        _refreshTwitchTokenTimer.Elapsed += (o, e) => RefreshTwitchToken();
        _refreshTwitchTokenTimer.Start();

        ServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(_twitchApi);
        serviceCollection.AddSingleton(new BroadcasterId(_twitchSecrets.BroadcasterId));
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

    public static async Task RefreshTwitchToken()
    {
        ValidateAccessTokenResponse response = await _twitchApi.Auth.ValidateAccessTokenAsync();

        if (response is not null && response.ExpiresIn > 180)
            return;

        Console.WriteLine("Refreshing the no longer valid / about to expire Twitch auth token");

        try
        {
            RefreshResponse refreshResponse = await _twitchApi.Auth.RefreshAuthTokenAsync(_twitchRefreshToken, _twitchSecrets.Secret, _twitchSecrets.ClientId);
            _twitchRefreshToken = refreshResponse.RefreshToken;
            _twitchApi.Settings.AccessToken = refreshResponse.AccessToken;
            _twitchSecrets.RefreshToken = refreshResponse.RefreshToken;
            _twitchSecrets.AccessToken = refreshResponse.AccessToken;
            _twitchSecrets.Save();
        }
        catch (Exception e)
        {
            Console.WriteLine("WARNING: Twitch token could not be refreshed, the application will probably stop working");
            Console.WriteLine(e);
        }
    }
}