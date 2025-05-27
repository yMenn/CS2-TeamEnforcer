using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace TeamEnforcer;

public class TeamEnforcerConfig : BasePluginConfig
{
    [JsonPropertyName("ChatMessagePrefix")] 
    public string ChatMessagePrefix { get; set; } = $" {ChatColors.DarkBlue}[{ChatColors.LightBlue}TeamEnforcer{ChatColors.DarkBlue}]{ChatColors.Default}";

    [JsonPropertyName("RoundsInCtToLowPrio")]
    public int RoundsInCtToLowPrio { get; set; } = 2;

    [JsonPropertyName("DefaultCTRatio")]
    public float DefaultCTRatio { get; set; } = 0.25f; // 1 CT for every 4 players

    [JsonPropertyName("DatabaseHost")]
    public string DatabaseHost { get; set; } = "";

    [JsonPropertyName("DatabasePort")]
    public int DatabasePort { get; set; } = 3306;

    [JsonPropertyName("DatabaseUser")]
    public string DatabaseUser { get; set; } = "";

    [JsonPropertyName("DatabasePassword")]
    public string DatabasePassword { get; set; } = "";

    [JsonPropertyName("DatabaseName")]
    public string DatabaseName { get; set; } = "";
}