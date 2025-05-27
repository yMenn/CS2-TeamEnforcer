using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using TeamEnforcer.Helpers;
using TeamEnforcer.Services;

namespace TeamEnforcer.Managers;

public class TeamManager(QueueManager queueManager, MessageService messageService, TeamEnforcer plugin, CTBanService? ctBanService)
{
    public readonly MessageService _messageService = messageService;
    public readonly CTBanService? _ctBanService = ctBanService;
    public readonly TeamEnforcer _plugin = plugin;

    private readonly Random _random = new();
    private readonly QueueManager _queueManager = queueManager;
    private readonly HashSet<CCSPlayerController> _noCtList = [];
    
    private readonly Stack<CCSPlayerController> ctJoinOrder = [];
    private readonly HashSet<CCSPlayerController> legitCtJoins = [];
    private readonly List<CCSPlayerController> _leaveCtList = [];

    private readonly List<ulong> _ctsThisMap = [];
    private List<ulong> _ctsLastMap = [];
    private readonly Dictionary<CCSPlayerController, int> _ctRoundCount = [];
    private readonly Dictionary<string, int> _ctKickedPlayers = [];


    public void PrepareForNewMap()
    {
        _queueManager.ClearQueues();
        _noCtList.Clear();
        _leaveCtList.Clear();
        legitCtJoins.Clear();
        ctJoinOrder.Clear();

        _ctsLastMap = new(_ctsThisMap);
        _ctsThisMap.Clear();
        _ctRoundCount.Clear();
        _ctKickedPlayers.Clear();
    }

    public int GetKickDuration(CCSPlayerController player)
    {
        if (player == null || !player.IsReal()) return 0;
        return _ctKickedPlayers.ContainsKey(player.SteamID.ToString()) ? _ctKickedPlayers[player.SteamID.ToString()] : 0;
    }

    public void KickPlayerFromCT(CCSPlayerController player, int duration)
    {
        if (player == null || !player.IsReal()) return;

        _ctKickedPlayers[player.SteamID.ToString()] = duration;
        DemoteToT(player);
    }

    public bool IsPlayerCTKicked(CCSPlayerController player)
    {
        if (player == null || !player.IsReal()) return false;
        return _ctKickedPlayers.ContainsKey(player.SteamID.ToString());
    }

    public void DecrementCTKickDurations()
    {
        var playersToRemove = new List<string>();

        foreach (var kvp in _ctKickedPlayers)
        {
            _ctKickedPlayers[kvp.Key]--;
            if (_ctKickedPlayers[kvp.Key] <= 0)
            {
                playersToRemove.Add(kvp.Key);
            }
        }

        foreach (var player in playersToRemove)
        {
            _ctKickedPlayers.Remove(player);
        }
    }
    
    public void AddToLeaveList(CCSPlayerController? player)
    {
        if (player == null || !player.IsReal()) return;

        if (player.Team != CsTeam.CounterTerrorist) return;

        _plugin.Logger.LogInformation("[TeamEnforcer] Adding {player} to leavers list.", player.PlayerName);

        if (_leaveCtList.Contains(player))
        {
            return;
        }

        _leaveCtList.Add(player);
        _messageService.PrintMessage(player, _plugin.Localizer["TeamEnforcer.AddedToLeaveList"]);
    }

    public bool IsInLeaveList(CCSPlayerController? player)
    {
        if (player == null || !player.IsReal()) return false;

        if (_leaveCtList.Contains(player))
        {
            return true;
        }
        return false;
    }
    
    public void BalanceTeams(bool warmupEnd)
    {
        _plugin.Logger.LogInformation("[TeamEnforcer] Balancing teams.");

        RemoveCTBannedCTs();
        RemoveLeavers();
        DemoteAnyIllegitimateCts(warmupEnd);

        int playersPromoted = 0;
        
        List<CCSPlayerController> players = Utilities.GetPlayers();

        int totalCtandT = players.FindAll(p => p.Team == CsTeam.Terrorist || p.Team == CsTeam.CounterTerrorist).Count;
        int ctCount = players.FindAll(p => p.Team == CsTeam.CounterTerrorist).Count;

        float ctRatio = _plugin.Config.DefaultCTRatio;
        int idealCtCount = (int) (totalCtandT * ctRatio);

        if (idealCtCount <= 0) idealCtCount = 1;
        
        if (ctCount < idealCtCount)
        {
            Console.WriteLine("[TeamEnforcer] Not enough CTs, getting more.");

            var promotionsNeeded = idealCtCount - ctCount;
            List<CCSPlayerController> promotionList = _queueManager.GetNextInQueue(promotionsNeeded);

            foreach (var player in promotionList)
            {
                if (player == null || !player.IsReal()) continue;
                PromoteToCt(player);
                _messageService.PrintToAll(_plugin.Localizer["TeamEnforcer.TPromotedFromQueue", player.PlayerName ?? "<John Doe>"]);
                playersPromoted++;
            }

            if (promotionList.Count < promotionsNeeded)
            {
                var randomsNeeded = promotionsNeeded - promotionList.Count;
                List<CCSPlayerController> randomsList = GetRandoms(randomsNeeded, CsTeam.Terrorist);
                foreach (var player in randomsList)
                {
                    PromoteToCt(player);
                    _messageService.PrintToAll(_plugin.Localizer["TeamEnforcer.RandomTPromoted", player.PlayerName ?? "<John Doe>"]);
                    playersPromoted++;
                }
            }

            if (playersPromoted < promotionsNeeded)
            {
                _messageService.PrintToAll(_plugin.Localizer["TeamEnforcer.NotEnoughTsAvailable"]);
            }
        }
        else if (ctCount > idealCtCount)
        {
            Console.WriteLine("[TeamEnforcer] Too many CTs, removing some.");

            int demotionsNeeded = ctCount - idealCtCount;

            int demotedCount = 0;

            demotedCount += DemoteAnyIllegitimateCts(false, demotionsNeeded);

            while (ctJoinOrder.Count > 0 && demotedCount < demotionsNeeded)
            {
                var nextCt = ctJoinOrder.Pop();

                if (nextCt == null || !nextCt.IsReal() || nextCt.Team != CsTeam.CounterTerrorist) continue;

                DemoteToT(nextCt);
                _queueManager.JoinQueue(nextCt, QueuePriority.High);
                _messageService.PrintToAll(_plugin.Localizer["TeamEnforcer.PlayerDemotedFromStack", nextCt.PlayerName ?? "<John Doe>"]);
                demotedCount++;
            }
        }
    }

    public bool WasCtLastMap(CCSPlayerController? player)
    {
        if (player == null || !player.IsReal()) return false;

        var steamId = player.SteamID;
        if (_ctsLastMap.Contains(steamId))
        {
            return true;
        }

        return false;
    }

    public void RemoveCTBannedCTs()
    {
        _plugin.Logger.LogInformation("[TeamEnforcer] Removing CTBanned cts.");

        if (_ctBanService == null) return;
        var ctBannedCTs = Utilities.GetPlayers().FindAll(p => p != null && p.IsReal() && p.Team == CsTeam.CounterTerrorist && _ctBanService.PlayerIsCTBanned(p));
        foreach (var ct in ctBannedCTs)
        {
            DemoteToT(ct);
            _messageService.PrintToAll(_plugin.Localizer["TeamEnforcer.RemovedBecauseCTBanned", ct.PlayerName]);
        }
    }

    public void RemoveLeavers()
    {
        _plugin.Logger.LogInformation("[TeamEnforcer] Removing CT leavers.");

        List<CCSPlayerController> successFulLeavers = [];
        foreach (var leaver in _leaveCtList)
        {
            if (leaver == null || !leaver.IsReal()) continue;
            if (leaver.Team == CsTeam.Terrorist) continue;

            successFulLeavers.Add(leaver);
            DemoteToT(leaver);
            JoinNoCtList(leaver);
            _messageService.PrintMessage(leaver, _plugin.Localizer["TeamEnforcer.DemotedFromLeaversList", "!t"]);
        }

        foreach (var leaver in successFulLeavers)
        {
            _leaveCtList.Remove(leaver);
        }
    }

    public int DemoteAnyIllegitimateCts(bool warmupEnd, int demotionsNeeded = 999)
    {
        _plugin.Logger.LogInformation("[TeamEnforcer] Removing illegitimate CTs.");

        var demotedCount = 0;
        var illegitimateCts = Utilities.GetPlayers().FindAll(p => p.Team == CsTeam.CounterTerrorist && !legitCtJoins.Contains(p));

        if (demotedCount < demotionsNeeded)
        {
            var illegitimateCtsToDemote = illegitimateCts.Take(demotionsNeeded);
            foreach (var ct in illegitimateCtsToDemote)
            {
                if (ct == null || !ct.IsReal()) continue;

                DemoteToT(ct);
                if (!warmupEnd)
                {
                    _messageService.PrintToAll(_plugin.Localizer["TeamEnforcer.PlayerDemotedJoinedIllegitimately", ct.PlayerName ?? "<John Doe>"]);
                }
                demotedCount++;
            }
        }

        return demotedCount;
    }

    public string GetLegitCtsString()
    {
        var message = new StringBuilder($"Legit joins ({legitCtJoins.Count}):");
        foreach (var player in legitCtJoins)
        {  
            if (player == null || !player.IsReal()) continue;
            message.Append($"- {player.PlayerName ?? "<John Doe>"}");
        }
        return message.ToString();
    }

    public void DemoteToT(CCSPlayerController? player)
    {
        if (player == null || !player.IsReal()) return;

        if (player.Team == CsTeam.Terrorist) return;

        _plugin.Logger.LogInformation("[TeamEnforcer] Demoting {player} to T.", player.PlayerName);

        legitCtJoins.Remove(player);
        player.SwitchTeam(CsTeam.Terrorist);
        player.CommitSuicide(false, true);
    }

    public void PromoteToCt(CCSPlayerController? player)
    {
        if (player == null || !player.IsReal()) return;

        if (player.Team == CsTeam.CounterTerrorist) return;

        if (_queueManager.IsPlayerInQueue(player, out var _))
        {
            _queueManager.LeaveQueue(player);
        }

        ctJoinOrder.Push(player);
        legitCtJoins.Add(player);
        player.SwitchTeam(CsTeam.CounterTerrorist);
        player.CommitSuicide(false, true);
    }

    public void UpdateMapCtList()
    {
        var cts = Utilities.GetPlayers().FindAll(p => p != null && p.IsReal() && p.Team == CsTeam.CounterTerrorist);

        foreach (var ct in cts)
        {
            if (ct == null || !ct.IsReal()) continue;

            if(!_ctRoundCount.TryGetValue(ct, out int roundCount))
            {
                if (!_ctRoundCount.ContainsKey(ct))
                {
                    _ctRoundCount[ct] = 0;
                }
            }

            if (roundCount >= _plugin.Config.RoundsInCtToLowPrio)
            {
                var steamId = ct.SteamID;

                if (!_ctsThisMap.Contains(steamId))
                {
                    _ctsThisMap.Add(steamId);
                    _messageService.PrintMessage(ct, _plugin.Localizer["TeamEnforcer.AddedToMapCtList"]);
                }
            }

            _ctRoundCount[ct] += 1;
        }
    }

    public List<CCSPlayerController> GetRandoms(int count, CsTeam team)
    {
        _plugin.Logger.LogInformation("[TeamEnforcer] Getting random T's to join CT.");

        List<CCSPlayerController> randomsList = new(count);
        var teamsPlayers = Utilities.GetPlayers()
            .FindAll(p => p != null && p.IsReal() && p.Team == team);

        if (team == CsTeam.Terrorist)
        {
            teamsPlayers = teamsPlayers.FindAll(p => !_noCtList.Contains(p) && !IsPlayerCTKicked(p));
        }

        var attempts = 0;
        while(randomsList.Count < count && teamsPlayers.Count > 0 && attempts < 50)
        {
            attempts ++;
            var randomIndex = _random.Next(teamsPlayers.Count);
            var randomPlayer = teamsPlayers[randomIndex];

            var isCtBanned = _ctBanService?.PlayerIsCTBanned(randomPlayer) ?? false;
            if (randomPlayer != null && randomPlayer.IsReal() && !isCtBanned)
            {
                randomsList.Add(randomPlayer);
                teamsPlayers.RemoveAt(randomIndex);
            }
        }

        return randomsList;
    }

    public void JoinNoCtList(CCSPlayerController? player)
    {
        if (player == null || !player.IsReal()) return;

        if (player.Team != CsTeam.Terrorist) return;

        if (_noCtList.Contains(player))
        {
            _messageService.PrintMessage(player, _plugin.Localizer["TeamEnforcer.AlreadyInNoCTList"]);
            return;
        }

        _noCtList.Add(player);
        _messageService.PrintMessage(player, _plugin.Localizer["TeamEnforcer.AddedToNoCTList"]);
    }
}