using DSharpPlus.Entities;
using TwitchLib.Api.Helix.Models.Predictions;

namespace SAESRPredictionsBot;

public class ResolveInteractivityContext
{
    public DiscordUser User;
    public Prediction Prediction;

    public const string CommonButtonId = "saesr-resolve-choice";
    public const string CancelButtonId = CommonButtonId + "-cancel";
}