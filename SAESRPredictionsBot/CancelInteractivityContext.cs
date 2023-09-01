using DSharpPlus.Entities;
using TwitchLib.Api.Helix.Models.Predictions;

namespace SAESRPredictionsBot;

public class CancelInteractivityContext
{
    public DiscordUser User;
    public Prediction Prediction;

    public const string YesButtonId = "saesr-cancel-yes-button";
    public const string NoButtonId = "saesr-cancel-no-button";
}