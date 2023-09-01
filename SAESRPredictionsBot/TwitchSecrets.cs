namespace SAESRPredictionsBot;

public class TwitchSecrets
{
    public string ClientId { get; set; }
    public string AccessToken { get; set; }
    public string Secret { get; set; }
    public string BroadcasterId { get; set; }
}

public class BroadcasterId
{
    public string Id { get; set; }

    public BroadcasterId(string id)
    {
        Id = id;
    }

    public static implicit operator string(BroadcasterId id) => id.Id;
}