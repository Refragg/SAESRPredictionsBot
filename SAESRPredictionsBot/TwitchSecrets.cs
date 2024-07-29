using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SAESRPredictionsBot;

public class TwitchSecrets
{
    private const string FileName = "/config/twitch-secrets";
    
    public string ClientId { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public string Secret { get; set; }
    public string BroadcasterId { get; set; }
    
    public static TwitchSecrets Load()
    {
        string json = File.ReadAllText(FileName);
        return JsonConvert.DeserializeObject<TwitchSecrets>(json)!;
    }
    
    public void Save()
    {
        string json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented
        });
        
        File.WriteAllText(FileName, json);
    }

    private TwitchSecrets()
    {
    }
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