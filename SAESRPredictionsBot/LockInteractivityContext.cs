using DSharpPlus.Entities;
using TwitchLib.Api.Helix.Models.Predictions;

namespace SAESRPredictionsBot;

public class LockInteractivityContext
{
    public DiscordUser User;
    public Prediction Prediction;

    public const string YesButtonId = "saesr-lock-yes-button";
    public const string NoButtonId = "saesr-lock-no-button";
}