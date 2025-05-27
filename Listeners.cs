using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using TeamEnforcer.Helpers;
using CounterStrikeSharp.API.Modules.Utils;
using TeamEnforcer.Services;

namespace TeamEnforcer;

public partial class TeamEnforcer
{
    private readonly Dictionary<CCSPlayerController, DateTime> lastJoinAttempt = []; 
    public void RegisterListeners()
    {
        AddCommandListener("jointeam", OnJoinTeamCommand);

        if (_teamManager != null) 
        {
            RegisterListener<Listeners.OnMapStart>((_) => {
                _teamManager.PrepareForNewMap();
                lastJoinAttempt.Clear();
            });
        }

        _messageService.PrintToConsole("Registered Listeners");
    }

    public HookResult OnJoinTeamCommand(CCSPlayerController? invoker, CommandInfo commandInfo)
    {
        if (invoker == null || !invoker.IsReal()) return HookResult.Continue;

        if (commandInfo.ArgByIndex(1) == "3") // Trying to join CT
        {
            // Allow only every 3 seconds so no sound and chat spam.
            if (!lastJoinAttempt.TryGetValue(invoker, out DateTime value) || DateTime.Now > value.AddSeconds(3))
            {
                _messageService.PrintMessage(
                    invoker,
                    Localizer["TeamEnforcer.CannotJoinCt", $"!guard"]
                );
                invoker.ExecuteClientCommand($"play sounds/ui/menu_invalid.vsnd_c");
            }
            lastJoinAttempt[invoker] = DateTime.Now;

            return HookResult.Handled;
        }

        if (commandInfo.ArgByIndex(1) == "2" && invoker.Team == CsTeam.CounterTerrorist) // Trying to join T from CT
        {
            // Allow only every 3 seconds so no sound and chat spam.
            if (!lastJoinAttempt.TryGetValue(invoker, out DateTime value) || DateTime.Now > value.AddSeconds(3))
            {
                if (_teamManager?.IsInLeaveList(invoker) ?? false)
                {
                    invoker.ExecuteClientCommand($"play sounds/ui/menu_invalid.vsnd_c");
                    _messageService.PrintMessage(invoker, Localizer["TeamEnforcer.AlreadyInLeaveList"]);
                }
                else
                {
                    invoker.ExecuteClientCommand($"play sounds/ui/menu_invalid.vsnd_c");
                    _teamManager?.AddToLeaveList(invoker);
                }
            }
            lastJoinAttempt[invoker] = DateTime.Now;

            return HookResult.Handled;
        }

        return HookResult.Continue;
    }
}