using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;
using TeamEnforcer.Helpers;
using TeamEnforcer.Services;

namespace TeamEnforcer;

public partial class TeamEnforcer
{
    # if DEBUG
    [ConsoleCommand("css_legitjoins")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/generic")]
    public void OnLegitJoinCommand(CCSPlayerController? invoker, CommandInfo commandInfo)
    {
        var msg = _teamManager?.GetLegitCtsString();

        commandInfo.ReplyToCommand(msg ?? "Unable to find list.");
    }
    # endif

    [ConsoleCommand("css_ctbaninfo")]
    [CommandHelper(minArgs: 1, usage: "<player_name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/ban")]
    public void OnCTBanInfoCommand(CCSPlayerController? invoker, CommandInfo commandInfo)
    {
        if (_ctBanService == null)
        {
            Logger.LogCritical("[TeamEnforcer] {invoker} attempted to use CTBan command but service was null.", invoker?.PlayerName ?? "Console");
            commandInfo.ReplyToCommand(_messageService.GetMessageString(Localizer["TeamEnforcer.CTBanUnavailable"]) ?? "[TeamEnforcer] CTBan features are unavailable.");
            return;
        }

        string targetString = commandInfo.ArgByIndex(1);
        if (targetString == string.Empty)
        {
            commandInfo.ReplyToCommand(_messageService.GetMessageString(Localizer["TeamEnforcer.InvalidTarget", "!ctban <player_name> [duration] [reason]"]) ?? "[TeamEnforcer] Invalid target. Usage: !ctban <player_name> [duration] [reason]");
            return;
        }

        CCSPlayerController? targetPlayer = Utils.FindTarget(targetString);
        if (targetPlayer == null || !targetPlayer.IsReal())
        {
            commandInfo.ReplyToCommand(_messageService.GetMessageString(Localizer["TeamEnforcer.TargetNotFound", targetString]) ?? $"[TeamEnforcer] Unable to find target: {targetString}");
            return;
        }

        Logger.LogInformation("[TeamEnforcer] {invoker} used !ctbaninfo, target: {target}. Timestamp: {date}", invoker?.PlayerName ?? "Console", targetPlayer.PlayerName, DateTime.UtcNow);
        Task.Run(async () => {
            bool isCtBanned = await _ctBanService.PlayerIsCTBannedAsync(targetPlayer);

            if (!isCtBanned)
            {
                Server.NextFrame(() => {
                    var fallbackMsg = $"[TeamEnforcer] {targetPlayer.PlayerName} does not have an active CTBan.";
                    var notCtBannedMsg = _messageService.GetMessageString(Localizer["TeamEnforcer.NotCTBanned", targetPlayer.PlayerName]) ?? fallbackMsg;
                    if (invoker != null)
                    {
                        invoker.PrintToChat(notCtBannedMsg);
                    }
                    else
                    {
                        Console.WriteLine(fallbackMsg);
                    }
                });
                return;
            }
            
            var ctBanInfo = await _ctBanService.GetCTBanInfoAsync(targetPlayer);
            if (ctBanInfo != null)
            {
                DateTime? expirationDate = ctBanInfo.ExpirationDate;
                TimeSpan? timeLeft = expirationDate - DateTime.UtcNow;
                TimeSpan? totalDuration = ctBanInfo.ExpirationDate - ctBanInfo.BanDate;

                Server.NextFrame(() => {
                    string message;
                    if (expirationDate == null) // Permanent ban case
                    {
                        message = _messageService.GetMessageString(
                            Localizer["TeamEnforcer.PlayerBanInfoPerm", targetPlayer.PlayerName, ctBanInfo.BanDate, ctBanInfo.StaffSteamId]
                        ) ?? $"[TeamEnforcer] {targetPlayer.PlayerName} is permanently banned from CT. Ban date: {ctBanInfo.BanDate}, Staff: {ctBanInfo.StaffSteamId}";
                    }
                    else // Temporary ban case
                    {
                        double roundedTotalMinutes = Math.Round(totalDuration!.Value.TotalMinutes);
                        double roundedMinutesLeft = Math.Round(timeLeft!.Value.TotalMinutes);
                        message = _messageService.GetMessageString(
                            Localizer["TeamEnforcer.PlayerBanInfoTemp", targetPlayer.PlayerName, ctBanInfo.BanDate, 
                            ctBanInfo.StaffSteamId, roundedTotalMinutes, roundedMinutesLeft]
                        ) ?? $"[TeamEnforcer] {targetPlayer.PlayerName} is banned from CT. Ban date: {ctBanInfo.BanDate}, Staff: {ctBanInfo.StaffSteamId}, Total duration: {roundedTotalMinutes} minutes, Time left: {roundedMinutesLeft} minutes";
                    }

                    if (invoker != null)
                    {
                        invoker.PrintToChat(message);
                    }
                    else
                    {
                        Console.WriteLine(message);
                    }
                });
            }
        });
    }

    [ConsoleCommand("css_ctban")]
    [CommandHelper(minArgs: 1, usage: "<player_name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/ban")]
    public void OnCTBanCommand(CCSPlayerController? invoker, CommandInfo commandInfo)
    {
        if (_ctBanService == null)
        {
            Logger.LogCritical("[TeamEnforcer] {invoker} attempted to use CTBan command but service was null.", invoker?.PlayerName ?? "Console");
            commandInfo.ReplyToCommand(_messageService.GetMessageString(Localizer["TeamEnforcer.CTBanUnavailable"]) ?? "[TeamEnforcer] CTBan features are unavailable.");
            return;
        }

        string targetString = commandInfo.ArgByIndex(1);
        if (targetString == string.Empty)
        {
            commandInfo.ReplyToCommand(_messageService.GetMessageString(Localizer["TeamEnforcer.InvalidTarget", "!ctban <player_name> [duration] [reason]"]) ?? "[TeamEnforcer] Invalid target. Usage: !ctban <player_name> [duration] [reason]");
            return;
        }

        CCSPlayerController? targetPlayer = Utils.FindTarget(targetString);
        if (targetPlayer == null || !targetPlayer.IsReal())
        {
            commandInfo.ReplyToCommand(_messageService.GetMessageString(Localizer["TeamEnforcer.TargetNotFound", targetString]) ?? $"[TeamEnforcer] Unable to find target: {targetString}");
            return;
        }

        var banDurationArg = uint.TryParse(commandInfo.ArgByIndex(2), out uint banDurationInt);
        var ctbanReason = commandInfo.ArgByIndex(3);
        if (ctbanReason == string.Empty) ctbanReason = "No reason provided.";

        Logger.LogInformation("[TeamEnforcer] CTBan attempt - Invoker: {Invoker}, Target: {Target}, Duration: {Duration}, Reason: {Reason}", 
            invoker?.PlayerName ?? "Console", targetPlayer.PlayerName, banDurationArg ? banDurationInt.ToString() : "Permanent", ctbanReason);

        Task.Run(async () => {
            bool isBannedAlready = await _ctBanService.PlayerIsCTBannedAsync(targetPlayer);

            if (isBannedAlready)
            {
                Server.NextFrame(() => {
                    var alreadyBannedMsg = _messageService.GetMessageString(Localizer["TeamEnforcer.AlreadyBanned", targetPlayer.PlayerName]) ?? $"[TeamEnforcer] {targetPlayer.PlayerName} is already CTBanned.";
                    if (invoker != null)
                    {
                        invoker.PrintToChat(alreadyBannedMsg);
                    }
                    else
                    {
                        Console.WriteLine(alreadyBannedMsg);
                    }
                });
                return;
            }

            string targetSteamIdString = targetPlayer.SteamID.ToString();
            if (string.IsNullOrEmpty(targetSteamIdString))
            {
                Server.NextFrame(() => {
                    var errorMsg = "[TeamEnforcer] Error: Target player does not have a valid SteamID.";
                    if (invoker != null)
                    {
                        invoker.PrintToChat(errorMsg);
                    }
                    else
                    {
                        Console.WriteLine(errorMsg);
                    }
                });
                return;
            }

            NewCTBan newBan = new()
            {
                PlayerSteamId = targetSteamIdString,
                StaffSteamId = invoker?.SteamID.ToString() ?? "Console",
                BanReason = ctbanReason,
                BanDate = DateTime.UtcNow,
                ExpirationDate = banDurationArg && banDurationInt > 0 ? DateTime.UtcNow.AddMinutes(banDurationInt) : null,
                Active = true
            };

            try
            {
                await _ctBanService.BanPlayerAsync(newBan);
                Server.NextFrame(() => {
                    var durationString = banDurationArg && banDurationInt > 0 ? $"{banDurationInt} minutes" : "permanent";
                    Server.PrintToChatAll(
                        _messageService.GetMessageString(Localizer["TeamEnforcer.BanSuccess", targetPlayer.PlayerName, durationString])
                        ?? $"[TeamEnforcer] {targetPlayer.PlayerName} was CTBanned. Duration: {durationString}"
                    );
                    _teamManager?.DemoteToT(targetPlayer);
                    _queueManager?.LeaveQueue(targetPlayer);
                });
            }
            catch
            {
                Server.NextFrame(() => {
                    var errorMsg = "[TeamEnforcer] Error while banning player asynchronously";
                    if (invoker != null)
                    {
                        invoker.PrintToChat(errorMsg);
                    }
                    else
                    {
                        Console.WriteLine(errorMsg);
                    }
                    Logger.LogCritical(errorMsg);
                });
            }
        });
    }

    [ConsoleCommand("css_ctunban")]
    [CommandHelper(minArgs: 1, usage: "<player_name> [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/ban")]
    public void OnCTUnbanCommand(CCSPlayerController? invoker, CommandInfo commandInfo)
    {
        if (_ctBanService == null)
        {
            Logger.LogCritical("[TeamEnforcer] {invoker} attempted to use CTBan command but service was null.", invoker?.PlayerName ?? "Console");
            commandInfo.ReplyToCommand(_messageService.GetMessageString(Localizer["TeamEnforcer.CTBanUnavailable"]) ?? "[TeamEnforcer] CTBan features are unavailable.");
            return;
        }

        string targetString = commandInfo.ArgByIndex(1);
        if (targetString == string.Empty)
        {
            commandInfo.ReplyToCommand(_messageService.GetMessageString(Localizer["TeamEnforcer.InvalidTarget", "!ctunban <player_name> [reason]"]) ?? "[TeamEnforcer] Invalid target. Usage: !ctunban <player_name> [reason]");
            return;
        }

        CCSPlayerController? targetPlayer = Utils.FindTarget(targetString);
        if (targetPlayer == null || !targetPlayer.IsReal())
        {
            commandInfo.ReplyToCommand(_messageService.GetMessageString(Localizer["TeamEnforcer.TargetNotFound", targetString]) ?? $"[TeamEnforcer] Unable to find target: {targetString}");
            return;
        }


        var unbanReason = commandInfo.ArgByIndex(2);
        if (unbanReason == string.Empty) unbanReason = "No reason provided.";

        Logger.LogInformation("[TeamEnforcer] CTUnban attempt - Invoker: {Invoker}, Target: {Target}, Reason: {Reason}", 
            invoker?.PlayerName ?? "Console", targetPlayer.PlayerName, unbanReason);

        Task.Run(async () => {
            var isPlayerBanned = await _ctBanService.PlayerIsCTBannedAsync(targetPlayer);
            var currentBan = await _ctBanService.GetCTBanInfoAsync(targetPlayer);

            if (!isPlayerBanned || currentBan == null)
            {
                Server.NextFrame(() => {
                    if (invoker != null)
                    {
                        var notCtBannedMsg = _messageService.GetMessageString(Localizer["TeamEnforcer.NotCTBanned", targetPlayer.PlayerName]);
                        invoker.PrintToChat(notCtBannedMsg);
                    }
                    else
                    {
                        Console.WriteLine($"[TeamEnforcer] {targetPlayer.PlayerName} does not have an active CTBan.");
                    }
                });
                return;
            }

            try {
                await _ctBanService.UnbanPlayerAsync(currentBan, invoker?.SteamID.ToString() ?? "Console", unbanReason);
                Server.NextFrame(() => {
                    Server.PrintToChatAll(
                        _messageService.GetMessageString(Localizer["TeamEnforcer.UnbanSuccess", targetPlayer.PlayerName])
                        ?? $"[TeamEnforcer] {targetPlayer.PlayerName} is no longer CTBanned."
                    );
                });
            }
            catch
            {
                Server.NextFrame(() => {
                    var errorMsg = "[TeamEnforcer] Error while unbanning player asynchronously.";
                    if (invoker != null)
                    {
                        invoker.PrintToChat(errorMsg);
                    }
                    else
                    {
                        Console.WriteLine(errorMsg);
                    }
                    Logger.LogCritical(errorMsg);
                });
            }
        });
    }

    [ConsoleCommand("css_forcect")]
    [CommandHelper(minArgs: 1, usage: "<player_name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/kick")]
    public void OnForceCTCommand(CCSPlayerController? invoker, CommandInfo commandInfo)
    {
        if (_teamManager == null)
        {
            commandInfo.ReplyToCommand(_messageService.GetMessageString(Localizer["TeamEnforcer.TeamManagerUnavailable"]));
            return;
        }

        string targetString = commandInfo.ArgByIndex(1);
        if (targetString == string.Empty)
        {
            commandInfo.ReplyToCommand(_messageService.GetMessageString(Localizer["TeamEnforcer.InvalidTarget", "!forcect <player_name>"]));
            return;
        }

        CCSPlayerController? targetPlayer = Utils.FindTarget(targetString);
        if (targetPlayer == null || !targetPlayer.IsReal())
        {
            commandInfo.ReplyToCommand(_messageService.GetMessageString(Localizer["TeamEnforcer.TargetNotFound", targetString]));
            return;
        }

        Logger.LogInformation("[TeamEnforcer] ForceMove to CT - Invoker: {Invoker}, Target: {Target}", 
            invoker?.PlayerName ?? "Console", targetPlayer.PlayerName);
            
        _teamManager.PromoteToCt(targetPlayer);
        _messageService.PrintToAll(Localizer["TeamEnforcer.PlayerForcedToCT", targetPlayer.PlayerName, invoker?.PlayerName ?? "Console"]);
    }
    
    [ConsoleCommand("css_ctkick")]
    [CommandHelper(minArgs: 1, usage: "<player_name/steam_id> [rounds_to_kick]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/kick")]
    public void OnCTKickCommand(CCSPlayerController? invoker, CommandInfo commandInfo)
    {
        if (_teamManager == null)
        {
            commandInfo.ReplyToCommand(_messageService.GetMessageString(Localizer["TeamEnforcer.TeamManagerUnavailable"]));
            return;
        }

        string targetString = commandInfo.ArgByIndex(1);
        if (targetString == string.Empty)
        {
            commandInfo.ReplyToCommand(_messageService.GetMessageString(Localizer["TeamEnforcer.InvalidTarget", "!ctkick <player_name> [roundes_to_kick]"]) ?? "[TeamEnforcer] Invalid target. Usage: !ctban <player_name> [duration] [reason]");
            return;
        }

        CCSPlayerController? targetPlayer = Utils.FindTarget(targetString);
        if (targetPlayer == null || !targetPlayer.IsReal())
        {
            commandInfo.ReplyToCommand(_messageService.GetMessageString(Localizer["TeamEnforcer.TargetNotFound", targetString]));
            return;
        }

        int roundsToKick = commandInfo.ArgCount > 2 ? int.Parse(commandInfo.ArgByIndex(2)) : 5;
        bool isCtKicked = _teamManager?.IsPlayerCTKicked(targetPlayer) ?? false;
        if (isCtKicked)
        {
            commandInfo.ReplyToCommand(_messageService.GetMessageString(Localizer["TeamEnforcer.AlreadyKicked", targetPlayer.PlayerName]));
            return;
        }

        _teamManager?.KickPlayerFromCT(targetPlayer, roundsToKick);
        _queueManager?.LeaveQueue(targetPlayer);
        commandInfo.ReplyToCommand(_messageService.GetMessageString(Localizer["TeamEnforcer.PlayerKickedFromCT", targetPlayer.PlayerName, invoker?.PlayerName ?? "Console"]));
        Logger.LogInformation("Player {Player} kicked from CT by {Invoker}. Time: {Time}", targetPlayer.PlayerName, invoker?.PlayerName ?? "Console", DateTime.Now);
    }

    [ConsoleCommand("css_removequeue")]
    [CommandHelper(minArgs: 1, usage: "<player_name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/generic")]
    public void OnRemoveFromQueueCommand(CCSPlayerController? invoker, CommandInfo commandInfo) 
    {
        string targetString = commandInfo.ArgByIndex(1);
        CCSPlayerController? targetPlayer = Utils.FindTarget(targetString);

        if (targetPlayer == null || !targetPlayer.IsReal())
        {
            commandInfo.ReplyToCommand(_messageService.GetMessageString(Localizer["TeamEnforcer.TargetNotFound", targetString]) ?? $"[TeamEnforcer] Unable to find target: {targetString}");
            return;
        }

        _queueManager?.LeaveQueue(targetPlayer);
        commandInfo.ReplyToCommand(_messageService.GetMessageString(Localizer["TeamEnforcer.PlayerForceRemovedFromQueue", targetPlayer.PlayerName, invoker?.PlayerName ?? "Console"]));
        Logger.LogInformation("Player {Player} removed from queue by {Invoker}", targetPlayer.PlayerName, invoker?.PlayerName ?? "Console");
    }
}