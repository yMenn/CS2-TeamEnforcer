using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;

namespace TeamEnforcer.Helpers;

public class Utils
{
    public static CCSPlayerController? FindTarget(string target)
    {
        List<CCSPlayerController> players = Utilities.GetPlayers().FindAll(p => p != null && p.IsReal());
        foreach (CCSPlayerController player in players)
        {
            if (player == null || !player.IsReal()) continue;

            if (player.PlayerName.Contains(target, StringComparison.CurrentCultureIgnoreCase))
            {
                return player;
            }
        }

        var steamId = new SteamID(target);
        if (steamId.IsValid())
        {
            var player = Utilities.GetPlayerFromSteamId(steamId.SteamId64);
            if (player != null && player.IsReal())
            {
                return player;
            }
        }

        return null;
    }
}