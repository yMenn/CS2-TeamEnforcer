using CounterStrikeSharp.API.Core;

namespace TeamEnforcer.Helpers;

public static class PlayerExtensions
{
    public static bool IsReal(this CCSPlayerController? player)
    {
        if (player == null) return false;
        // if (!player.IsValid || player.IsBot || player.IsHLTV || player.Connected != PlayerConnectedState.PlayerConnected) return false;
        if (!player.IsValid || player.IsHLTV || player.Connected != PlayerConnectedState.PlayerConnected) return false;
        return true;
    }
}