using DSharpPlus.Entities;
using TwitchLib.Api.Helix.Models.Predictions.CreatePrediction;

namespace SAESRPredictionsBot;

public class CreateInteractivityContext
{
    public DiscordUser User;
    public CreatePredictionRequest PredictionRequest;

    public const string YesButtonId = "saesr-create-yes-button";
    public const string CancelButtonId = "saesr-create-cancel-button";
}