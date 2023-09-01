using System.Globalization;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Humanizer;
using Microsoft.Extensions.Logging;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Predictions;
using TwitchLib.Api.Helix.Models.Predictions.CreatePrediction;
using TwitchLib.Api.Helix.Models.Predictions.GetPredictions;
using Outcome = TwitchLib.Api.Helix.Models.Predictions.Outcome;
using CreateOutcome = TwitchLib.Api.Helix.Models.Predictions.CreatePrediction.Outcome;

namespace SAESRPredictionsBot;

public class PredictionsCommands : ApplicationCommandModule
{
    public static PredictionsCommands Instance;
    
    private TwitchAPI _twitchApi;
    private BroadcasterId _broadcasterId;
    private DiscordSecrets _discordSecrets;
    
    private static readonly DiscordColor InfoColor = new DiscordColor(0, 100, 255);
    private static readonly DiscordColor WarningColor = new DiscordColor(200, 200, 0);
    private static readonly DiscordColor ErrorColor = new DiscordColor(175, 0, 0);

    private static CreateInteractivityContext? _createInteractivityContext = default;
    private static CancelInteractivityContext? _cancelInteractivityContext = default;
    private static LockInteractivityContext? _lockInteractivityContext = default;
    private static ResolveInteractivityContext? _resolveInteractivityContext = default;
    
    public PredictionsCommands(TwitchAPI twitchApi, BroadcasterId broadcasterId, DiscordSecrets discordSecrets)
    {
        _twitchApi = twitchApi;
        _broadcasterId = broadcasterId;
        _discordSecrets = discordSecrets;
        Instance = this;
    }

    public async Task HandleCreateInteractivity(DiscordClient sender,
        ComponentInteractionCreateEventArgs eventArgs)
    {
        if (_createInteractivityContext is null)
        {
            sender.Logger.LogWarning("Unknown component interaction on message " + eventArgs.Message);
            return;
        }

        if (eventArgs.User != _createInteractivityContext.User)
        {
            await eventArgs.Channel.SendMessageAsync(new DiscordMessageBuilder().WithContent(eventArgs.User.Mention +
                " Only the command initiator can answer the question."));
            return;
        }

        DiscordInteractionResponseBuilder response;

        if (eventArgs.Interaction.Data.CustomId.StartsWith(CreateInteractivityContext.YesButtonId))
        {
            CreatePredictionRequest predictionRequest = _createInteractivityContext.PredictionRequest;

            await _twitchApi.Helix.Predictions.CreatePredictionAsync(predictionRequest);

            response = new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("New prediction created")
                    .WithDescription("The new prediction was created")
                    .WithColor(InfoColor));
        }
        else if (eventArgs.Interaction.Data.CustomId == CreateInteractivityContext.CancelButtonId)
        {
            response = new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Operation canceled")
                    .WithDescription("The create operation was canceled")
                    .WithColor(InfoColor));
        }
        else
        {
            response = new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Internal error")
                    .WithDescription("Couldn't create the new prediction due to a internal error")
                    .WithColor(ErrorColor));
        }

        _createInteractivityContext = default;

        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, response);
    }

    public async Task HandleCancelInteractivity(DiscordClient sender,
        ComponentInteractionCreateEventArgs eventArgs)
    {
        if (_cancelInteractivityContext is null)
        {
            sender.Logger.LogWarning("Unknown component interaction on message " + eventArgs.Message);
            return;
        }

        if (eventArgs.User != _cancelInteractivityContext.User)
        {
            await eventArgs.Channel.SendMessageAsync(new DiscordMessageBuilder().WithContent(eventArgs.User.Mention +
                " Only the command initiator can answer the question."));
            return;
        }

        DiscordInteractionResponseBuilder response;

        if (eventArgs.Interaction.Data.CustomId == CancelInteractivityContext.YesButtonId)
        {
            Prediction prediction = _cancelInteractivityContext.Prediction;

            await _twitchApi.Helix.Predictions.EndPredictionAsync(prediction.BroadcasterId, prediction.Id,
                PredictionEndStatus.CANCELED);

            response = new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Prediction canceled")
                    .WithDescription("The current prediction was canceled")
                    .WithColor(InfoColor));
        }
        else if (eventArgs.Interaction.Data.CustomId == CancelInteractivityContext.NoButtonId)
        {
            response = new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Operation canceled")
                    .WithDescription("The cancel operation was canceled")
                    .WithColor(InfoColor));
        }
        else
        {
            response = new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Internal error")
                    .WithDescription("Couldn't cancel the current prediction due to a internal error")
                    .WithColor(ErrorColor));
        }

        _cancelInteractivityContext = default;

        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, response);
    }

    public async Task HandleLockInteractivity(DiscordClient sender,
        ComponentInteractionCreateEventArgs eventArgs)
    {
        if (_lockInteractivityContext is null)
        {
            sender.Logger.LogWarning("Unknown component interaction on message " + eventArgs.Message);
            return;
        }

        if (eventArgs.User != _lockInteractivityContext.User)
        {
            await eventArgs.Channel.SendMessageAsync(new DiscordMessageBuilder().WithContent(eventArgs.User.Mention +
                " Only the command initiator can answer the question."));
            return;
        }

        DiscordInteractionResponseBuilder response;

        if (eventArgs.Interaction.Data.CustomId == LockInteractivityContext.YesButtonId)
        {
            Prediction prediction = _lockInteractivityContext.Prediction;

            await _twitchApi.Helix.Predictions.EndPredictionAsync(prediction.BroadcasterId, prediction.Id,
                PredictionEndStatus.LOCKED);

            response = new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Prediction locked")
                    .WithDescription("The current prediction was locked")
                    .WithColor(InfoColor));
        }
        else if (eventArgs.Interaction.Data.CustomId == LockInteractivityContext.NoButtonId)
        {
            response = new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Operation canceled")
                    .WithDescription("The lock operation was canceled")
                    .WithColor(InfoColor));
        }
        else
        {
            response = new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Internal error")
                    .WithDescription("Couldn't lock the current prediction due to a internal error")
                    .WithColor(ErrorColor));
        }

        _lockInteractivityContext = default;

        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, response);
    }

    public async Task HandleResolveInteractivity(DiscordClient sender,
        ComponentInteractionCreateEventArgs eventArgs)
    {
        if (_resolveInteractivityContext is null)
        {
            sender.Logger.LogWarning("Unknown component interaction on message " + eventArgs.Message);
            return;
        }

        if (eventArgs.User != _resolveInteractivityContext.User)
        {
            await eventArgs.Channel.SendMessageAsync(new DiscordMessageBuilder().WithContent(eventArgs.User.Mention +
                " Only the command initiator can answer the question."));
            return;
        }

        DiscordInteractionResponseBuilder response;

        if (eventArgs.Interaction.Data.CustomId.StartsWith(ResolveInteractivityContext.CommonButtonId)
            && char.IsDigit(eventArgs.Interaction.Data.CustomId.Last()))
        {
            int choiceNumber = int.Parse(eventArgs.Interaction.Data.CustomId.Last().ToString());
            
            Prediction prediction = _resolveInteractivityContext.Prediction;

            await _twitchApi.Helix.Predictions.EndPredictionAsync(prediction.BroadcasterId, prediction.Id,
                PredictionEndStatus.RESOLVED, winningOutcomeId: prediction.Outcomes[choiceNumber].Id);

            response = new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Prediction resolved")
                    .WithDescription($"The current prediction was resolved with the choice '{prediction.Outcomes[choiceNumber].Title}'")
                    .WithColor(InfoColor));
        }
        else if (eventArgs.Interaction.Data.CustomId == ResolveInteractivityContext.CancelButtonId)
        {
            response = new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Operation canceled")
                    .WithDescription("The resolve operation was canceled")
                    .WithColor(InfoColor));
        }
        else
        {
            response = new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Internal error")
                    .WithDescription("Couldn't resolve the current prediction due to a internal error")
                    .WithColor(ErrorColor));
        }

        _resolveInteractivityContext = default;

        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, response);
    }

    [SlashRequireGuild]
    [SlashCommand("showcurrentprediction", "Shows information about the current prediction")]
    public async Task ShowCurrentPrediction(InteractionContext ctx)
    {
        if (!IsAllowedToRunCommand(ctx, _discordSecrets.LowestAllowedRoleId))
        {
            InformNotAllowedUser(ctx);
            return;
        }

        GetPredictionsResponse predictionsResponse =
            await _twitchApi.Helix.Predictions.GetPredictionsAsync(_broadcasterId.Id, first: 1);

        DiscordMessageBuilder responseMessageBuilder = new DiscordMessageBuilder();
        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
        
        if (predictionsResponse.Data.Length == 0)
        {
            embedBuilder = embedBuilder
                .WithTitle("Error")
                .WithDescription("Couldn't retrieve predictions on the channel")
                .WithColor(ErrorColor);
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder(responseMessageBuilder.WithEmbed(embedBuilder)));
            return;
        }
        
        if (predictionsResponse.Data[0].Status is not PredictionStatus.ACTIVE and not PredictionStatus.LOCKED)
        {
            embedBuilder = embedBuilder
                .WithTitle("Warning")
                .WithDescription("There is no prediction currently running")
                .WithColor(WarningColor);
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder(responseMessageBuilder.WithEmbed(embedBuilder)));
            return;
        }

        Prediction prediction = predictionsResponse.Data[0];

        string predictionStatus = prediction.Status switch
        {
            PredictionStatus.ACTIVE => "Active - closes in " + GetRemainingPredictionTime(prediction).Humanize(precision: 2),
            PredictionStatus.LOCKED => "Locked " + DateTime.Parse(prediction.LockedAt).Humanize(),
            _ => "error"
        };
        
        embedBuilder = embedBuilder
            .WithTitle("Current prediction:")
            .WithColor(InfoColor)
            .WithDescription(prediction.Title)
            .AddField("Status:", predictionStatus);

        foreach (Outcome outcome in prediction.Outcomes)
        {
            embedBuilder.AddField("Choice", outcome.Title, true);
        }
        
        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder(responseMessageBuilder.WithEmbed(embedBuilder)));
    }

    private (bool wasLongerThan25Chars, List<string> choices) ValidateAndGetChoices(params string?[] choices)
    {
        bool wasLongerThan25Chars = false;
        List<string> outputChoices = new List<string>();
        
        for (int i = 0; i < choices.Length; i++)
        {
            if (string.IsNullOrEmpty(choices[i]))
                break;

            if (choices[i].Length >= 25)
            {
                wasLongerThan25Chars = true;
                break;
            }
            
            outputChoices.Add(choices[i]);
        }

        return (wasLongerThan25Chars, outputChoices);
    }

    [SlashRequireGuild]
    [SlashCommand("createprediction", "Creates a twitch prediction")]
    public async Task CreatePredictionCommand(InteractionContext ctx,
        [Option("title", "The title of the prediction (between 1 and 45 characters)")] string predictionTitle,
        [Option("predictionWindow", "The number of minutes the prediction will stay open for (between 1 and 30)")] long predictionWindow,
        [Option("first", "The first choice (choices are between 1 and 25 characters)")] string firstChoice,
        [Option("second", "The second choice")] string secondChoice,
        [Option("third", "The third choice")] string? thirdChoice = default,
        [Option("fourth", "The fourth choice")] string? fourthChoice = default,
        [Option("fifth", "The fifth choice")] string? fifthChoice = default,
        [Option("sixth", "The sixth choice")] string? sixthChoice = default,
        [Option("seventh", "The seventh choice")] string? seventhChoice = default,
        [Option("eighth", "The eighth choice")] string? eighthChoice = default,
        [Option("ninth", "The ninth choice")] string? ninthChoice = default,
        [Option("tenth", "The tenth choice")] string? tenthChoice = default)
    {
        if (!IsAllowedToRunCommand(ctx, _discordSecrets.LowestAllowedRoleId))
        {
            InformNotAllowedUser(ctx);
            return;
        }
        
        (bool longerThan25Chars, List<string> choices) = ValidateAndGetChoices(firstChoice, secondChoice, thirdChoice, fourthChoice, fifthChoice, sixthChoice,
            seventhChoice, eighthChoice, ninthChoice, tenthChoice);
        
        DiscordMessageBuilder responseMessageBuilder = new DiscordMessageBuilder();
        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();

        if (predictionTitle.Length >= 45)
        {
            embedBuilder = embedBuilder
                .WithTitle("Warning")
                .WithDescription("Prediction title must be between 1 and 45 characters")
                .WithColor(WarningColor);
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder(responseMessageBuilder.WithEmbed(embedBuilder)));
            return;
        }
        if (predictionWindow < 1 || predictionWindow > 30)
        {
            embedBuilder = embedBuilder
                .WithTitle("Warning")
                .WithDescription("Prediction window must be between 1 and 30 minutes")
                .WithColor(WarningColor);
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder(responseMessageBuilder.WithEmbed(embedBuilder)));
            return;
        }
        if (longerThan25Chars)
        {
            embedBuilder = embedBuilder
                .WithTitle("Warning")
                .WithDescription("Prediction choice cannot be longer than 25 characters")
                .WithColor(WarningColor);
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder(responseMessageBuilder.WithEmbed(embedBuilder)));
            return;
        }
        if (choices.Count < 2)
        {
            embedBuilder = embedBuilder
                .WithTitle("Warning")
                .WithDescription($"{choices.Count} valid choices were provided, the choices must be a non empty string that is less than 25 characters")
                .WithColor(WarningColor);
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder(responseMessageBuilder.WithEmbed(embedBuilder)));
            return;
        }
        
        DiscordButtonComponent yesButton =
            new DiscordButtonComponent(ButtonStyle.Primary, CreateInteractivityContext.YesButtonId, "Yes");
        
        DiscordButtonComponent cancelButton =
            new DiscordButtonComponent(ButtonStyle.Danger, CreateInteractivityContext.CancelButtonId, "Cancel");

        DiscordInteractionResponseBuilder response = new DiscordInteractionResponseBuilder()
            .WithTitle("Create prediction")
            .WithContent("Are you sure that you want to create a new prediction with these settings?\n" +
                         $"Title: '{predictionTitle}'\n" +
                         $"Prediction time: {predictionWindow} minutes\n" +
                         "Choices:\n" +
                         $"- {string.Join("\n- ", choices)}")
            .AddComponents(yesButton, cancelButton);

        _createInteractivityContext = new CreateInteractivityContext
        {
            PredictionRequest = new CreatePredictionRequest
            {
                PredictionWindowSeconds = (int)predictionWindow * 60,
                BroadcasterId = _broadcasterId,
                Title = predictionTitle,
                Outcomes = choices.Select(x => new CreateOutcome
                {
                    Title = x
                }).ToArray()
            },
            User = ctx.User
        };
        
        await ctx.CreateResponseAsync(response);
    }
    
    [SlashRequireGuild]
    [SlashCommand("cancelcurrentprediction", "Cancels the currently running prediction")]
    public async Task CancelCurrentPrediction(InteractionContext ctx)
    {
        if (!IsAllowedToRunCommand(ctx, _discordSecrets.LowestAllowedRoleId))
        {
            InformNotAllowedUser(ctx);
            return;
        }

        GetPredictionsResponse predictionsResponse =
            await _twitchApi.Helix.Predictions.GetPredictionsAsync(_broadcasterId.Id, first: 1);
        
        DiscordMessageBuilder responseMessageBuilder = new DiscordMessageBuilder();
        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
        
        if (predictionsResponse.Data.Length == 0)
        {
            embedBuilder = embedBuilder
                .WithTitle("Error")
                .WithDescription("Couldn't retrieve predictions on the channel")
                .WithColor(ErrorColor);
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder(responseMessageBuilder.WithEmbed(embedBuilder)));
            return;
        }
        
        if (predictionsResponse.Data[0].Status is not PredictionStatus.ACTIVE and not PredictionStatus.LOCKED)
        {
            embedBuilder = embedBuilder
                .WithTitle("Warning")
                .WithDescription("There is no prediction currently running")
                .WithColor(WarningColor);
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder(responseMessageBuilder.WithEmbed(embedBuilder)));
            return;
        }

        Prediction prediction = predictionsResponse.Data[0];

        DiscordButtonComponent yesButton =
            new DiscordButtonComponent(ButtonStyle.Primary, CancelInteractivityContext.YesButtonId, "Yes");
        
        DiscordButtonComponent noButton =
            new DiscordButtonComponent(ButtonStyle.Danger, CancelInteractivityContext.NoButtonId, "No");

        DiscordInteractionResponseBuilder response = new DiscordInteractionResponseBuilder()
            .WithTitle("Cancel current prediction")
            .WithContent("Are you sure that you want to cancel the current prediction and refund the channel points?")
            .AddComponents(yesButton, noButton);
        
        _cancelInteractivityContext = new CancelInteractivityContext()
        {
            Prediction = prediction,
            User = ctx.User
        };
        
        await ctx.CreateResponseAsync(response);
    }

    [SlashRequireGuild]
    [SlashCommand("lockcurrentprediction", "Locks the currently running prediction")]
    public async Task LockCurrentPrediction(InteractionContext ctx)
    {
        if (!IsAllowedToRunCommand(ctx, _discordSecrets.LowestAllowedRoleId))
        {
            InformNotAllowedUser(ctx);
            return;
        }

        GetPredictionsResponse predictionsResponse =
            await _twitchApi.Helix.Predictions.GetPredictionsAsync(_broadcasterId.Id, first: 1);
        
        DiscordMessageBuilder responseMessageBuilder = new DiscordMessageBuilder();
        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
        
        if (predictionsResponse.Data.Length == 0)
        {
            embedBuilder = embedBuilder
                .WithTitle("Error")
                .WithDescription("Couldn't retrieve predictions on the channel")
                .WithColor(ErrorColor);
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder(responseMessageBuilder.WithEmbed(embedBuilder)));
            return;
        }
        
        if (predictionsResponse.Data[0].Status is not PredictionStatus.ACTIVE and not PredictionStatus.LOCKED)
        {
            embedBuilder = embedBuilder
                .WithTitle("Warning")
                .WithDescription("There is no prediction currently running")
                .WithColor(WarningColor);
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder(responseMessageBuilder.WithEmbed(embedBuilder)));
            return;
        }

        if (predictionsResponse.Data[0].Status == PredictionStatus.LOCKED)
        {
            embedBuilder = embedBuilder
                .WithTitle("Warning")
                .WithDescription("The current prediction is already locked")
                .WithColor(WarningColor);
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder(responseMessageBuilder.WithEmbed(embedBuilder)));
            return;
        }

        Prediction prediction = predictionsResponse.Data[0];

        DiscordButtonComponent yesButton =
            new DiscordButtonComponent(ButtonStyle.Primary, LockInteractivityContext.YesButtonId, "Yes");
        
        DiscordButtonComponent noButton =
            new DiscordButtonComponent(ButtonStyle.Danger, LockInteractivityContext.NoButtonId, "No");

        DiscordInteractionResponseBuilder response = new DiscordInteractionResponseBuilder()
            .WithTitle("Lock current prediction")
            .WithContent("Are you sure that you want to lock the current prediction?")
            .AddComponents(yesButton, noButton);
        
        _lockInteractivityContext = new LockInteractivityContext
        {
            Prediction = prediction,
            User = ctx.User
        };
        
        await ctx.CreateResponseAsync(response);
    }

    [SlashRequireGuild]
    [SlashCommand("resolvecurrentprediction", "Resolves the currently locked prediction")]
    public async Task ResolveCurrentPrediction(InteractionContext ctx)
    {
        if (!IsAllowedToRunCommand(ctx, _discordSecrets.LowestAllowedRoleId))
        {
            InformNotAllowedUser(ctx);
            return;
        }

        GetPredictionsResponse predictionsResponse =
            await _twitchApi.Helix.Predictions.GetPredictionsAsync(_broadcasterId.Id, first: 1);
        
        DiscordMessageBuilder responseMessageBuilder = new DiscordMessageBuilder();
        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
        
        if (predictionsResponse.Data.Length == 0)
        {
            embedBuilder = embedBuilder
                .WithTitle("Error")
                .WithDescription("Couldn't retrieve predictions on the channel")
                .WithColor(ErrorColor);
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder(responseMessageBuilder.WithEmbed(embedBuilder)));
            return;
        }
        
        if (predictionsResponse.Data[0].Status is not PredictionStatus.ACTIVE and not PredictionStatus.LOCKED)
        {
            embedBuilder = embedBuilder
                .WithTitle("Warning")
                .WithDescription("There is no prediction currently running")
                .WithColor(WarningColor);
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder(responseMessageBuilder.WithEmbed(embedBuilder)));
            return;
        }

        if (predictionsResponse.Data[0].Status == PredictionStatus.ACTIVE)
        {
            embedBuilder = embedBuilder
                .WithTitle("Warning")
                .WithDescription("The current prediction is still running")
                .WithColor(WarningColor);
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder(responseMessageBuilder.WithEmbed(embedBuilder)));
            return;
        }

        Prediction prediction = predictionsResponse.Data[0];

        List<DiscordButtonComponent> buttonComponents = new List<DiscordButtonComponent>();

        for (var i = 0; i < prediction.Outcomes.Length; i++)
        {
            var outcome = prediction.Outcomes[i];
            buttonComponents.Add(new DiscordButtonComponent(ButtonStyle.Secondary,
                ResolveInteractivityContext.CommonButtonId + i, outcome.Title));
        }

        DiscordInteractionResponseBuilder response = new DiscordInteractionResponseBuilder()
            .WithTitle("Resolve current prediction")
            .WithContent("Pick the choice to resolve the prediction");

        if (buttonComponents.Count > 5)
        {
            response.AddComponents(buttonComponents.Take(5));
            response.AddComponents(buttonComponents.Skip(5));
        }
        else
            response.AddComponents(buttonComponents);

        response.AddComponents(new DiscordButtonComponent(ButtonStyle.Danger,
            ResolveInteractivityContext.CancelButtonId, "Cancel"));
        
        _resolveInteractivityContext = new ResolveInteractivityContext()
        {
            Prediction = prediction,
            User = ctx.User
        };
        
        await ctx.CreateResponseAsync(response);
    }

    private TimeSpan GetRemainingPredictionTime(Prediction prediction)
    {
        DateTime creationTime = DateTime.Parse(prediction.CreatedAt);
        DateTime now = DateTime.UtcNow;
        TimeSpan predictionWindow = TimeSpan.FromSeconds(int.Parse(prediction.PredictionWindow));

        return predictionWindow - now.Subtract(creationTime);
    }

    private bool IsAllowedToRunCommand(InteractionContext ctx, ulong lowestAllowedRole)
    {
        if (!ctx.Guild.Roles.TryGetValue(lowestAllowedRole, out DiscordRole? role))
            return false;
        
        return ((DiscordMember)ctx.User).Hierarchy >= role.Position;
    }

    private void InformNotAllowedUser(InteractionContext ctx)
    {
        ctx.CreateResponseAsync(
            new DiscordInteractionResponseBuilder().WithContent(ctx.User.Mention + " You are not authorized to run this command"));
    }
}