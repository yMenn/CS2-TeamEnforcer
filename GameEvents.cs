using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace TeamEnforcer;

public partial class TeamEnforcer
{

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo eventInfo)
    {
        _teamManager?.UpdateMapCtList();
        _teamManager?.DecrementCTKickDurations();
        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo eventInfo)
    {
        _teamManager?.BalanceTeams(false);
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnWarmupEnd(EventWarmupEnd @event, GameEventInfo eventInfo)
    {
        _teamManager?.BalanceTeams(true);
        return HookResult.Continue;
    }
}
