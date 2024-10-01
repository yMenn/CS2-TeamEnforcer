using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using TeamEnforcer.Helpers;

namespace TeamEnforcer;

public partial class TeamEnforcer
{
    [ConsoleCommand("css_ct")]
    [ConsoleCommand("css_guard")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnGuardCommand(CCSPlayerController? invoker, CommandInfo commandInfo)
    {
        if (invoker == null || !invoker.IsReal()) return;
        if (invoker.Team != CsTeam.Terrorist)
        {
            _messageService?.PrintMessage(invoker, Localizer["TeamEnforcer.CannotJoinQueueFromNotT"]);
            return;
        }
        
        if (invoker.Team != CsTeam.Terrorist)
        {
            _messageService?.PrintMessage(invoker, Localizer["TeamEnforcer.CannotJoinQueueFromNotT"]);
            return;
        }

        var ctCount = Utilities.GetPlayers().FindAll(p => p != null && p.IsReal() && p.Team == CsTeam.CounterTerrorist).Count;

        if (ctCount == 0)
        {
            _teamManager?.PromoteToCt(invoker);
            _messageService?.PrintMessage(invoker, Localizer["TeamEnforcer.CtTeamEmptyInstantlyMoved"]);
            return;
        }

        if (_ctBanService == null)
        {
            var queuePrio = _teamManager?.WasCtLastMap(invoker)?? false ? Managers.QueuePriority.Low : Managers.QueuePriority.Normal;
            _queueManager?.JoinQueue(invoker, queuePrio);
            return;
        }

        Task.Run(async () => {
            var isCtBanned = await _ctBanService.PlayerIsCTBannedAsync(invoker);
            if (isCtBanned)
            {
                var banStatus = await _ctBanService.GetCTBanInfoAsync(invoker);
                if (banStatus == null) return;

                var banIsPermanent = banStatus.ExpirationDate == null;

                if (banIsPermanent)
                {
                    Server.NextFrame(() => {
                        invoker.PrintToChat(_messageService?.GetMessageString(Localizer["TeamEnforcer.CTBannedPermMessage"]) ?? "[TeamEnforcer] You are permanently banned from CT so you cannot join the queue.");
                    });
                    return;
                }

                TimeSpan? timeLeft = banStatus.ExpirationDate - DateTime.UtcNow;
                int minutesLeft = 0;
                if (timeLeft != null)
                {
                    minutesLeft = Math.Max(0, (int)timeLeft.Value.TotalMinutes);
                }
                Server.NextFrame(() => {
                    invoker.PrintToChat(_messageService?.GetMessageString(Localizer["TeamEnforcer.CTBannedTempMessage", minutesLeft]) ?? $"You are currently CTBanned and cannot join the CT team. Your ban will expire in {minutesLeft} minutes");
                });
                return;
            }

            Server.NextFrame(() => {
                var queuePrio = _teamManager?.WasCtLastMap(invoker)?? false ? Managers.QueuePriority.Low : Managers.QueuePriority.Normal;
                _queueManager?.JoinQueue(invoker, queuePrio);
            });
            return;
        });
    }

    [ConsoleCommand("css_t")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnTCommand(CCSPlayerController? invoker, CommandInfo commandInfo)
    {
        if (invoker == null || !invoker.IsReal() || invoker.Team != CsTeam.CounterTerrorist) return;

        _teamManager?.AddToLeaveList(invoker);
    }

    [ConsoleCommand("css_noct")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnNoctCommand(CCSPlayerController? invoker, CommandInfo commandInfo)
    {
        if (invoker == null || !invoker.IsReal()) return;

        if (invoker.Team == CsTeam.CounterTerrorist)
        {
            commandInfo.ReplyToCommand(_messageService?.GetMessageString(Localizer["TeamEnforcer.MustBeT"]) ?? "");
            return;
        }

        _teamManager?.JoinNoCtList(invoker);
        if (_queueManager?.IsPlayerInQueue(invoker, out var _) ?? false)
        {
            _queueManager.LeaveQueue(invoker);
        }
        
    }

    [ConsoleCommand("css_lq")]
    [ConsoleCommand("css_leavequeue")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnLeaveQueueCommand(CCSPlayerController? invoker, CommandInfo commandInfo)
    {
        if (invoker == null || !invoker.IsReal()) return;

        if (!_queueManager?.IsPlayerInQueue(invoker, out var _) ?? true)
        {
            commandInfo.ReplyToCommand(_messageService?.GetMessageString(Localizer["TeamEnforcer.NotInQueue"]) ?? "");
            return;
        }

        _queueManager?.LeaveQueue(invoker);
    }

    [ConsoleCommand("css_vq")]
    [ConsoleCommand("css_queue")]
    [ConsoleCommand("css_viewqueue")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnViewQueueCommand(CCSPlayerController? invoker, CommandInfo commandInfo)
    {
        // Print position if invoker in queue
        if (_queueManager?.IsQueueEmpty() ?? true)
        {
            commandInfo.ReplyToCommand(_messageService?.GetMessageString(Localizer["TeamEnforcer.QueueEmpty", "!guard"]) ?? "");
            return;
        }

        if (_queueManager.IsPlayerInQueue(invoker, out var playerQueueStatus))
        {
            commandInfo.ReplyToCommand(_messageService?.GetMessageString(Localizer["TeamEnforcer.YourPlaceInQueue", playerQueueStatus?.queuePosition ?? -1]) ?? "");
        }

        var queueStatus = _queueManager?.GetQueueStatus();

        commandInfo.ReplyToCommand(_messageService?.GetMessageString(queueStatus ?? "") ?? "");
    }
}