using System.Text;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using TeamEnforcer.Collections;
using TeamEnforcer.Helpers;
using TeamEnforcer.Services;

namespace TeamEnforcer.Managers;

public class QueueManager(MessageService messageService, IStringLocalizer localizer)
{
    public readonly MessageService _messageService = messageService;
    public readonly IStringLocalizer _localizer = localizer;

    private readonly CustomQueue<CCSPlayerController> _priorityQueue = new("Priority Queue");
    private readonly CustomQueue<CCSPlayerController> _mainQueue = new("Main Queue");
    private readonly CustomQueue<CCSPlayerController> _lowPriorityQueue = new("Low Priority Queue");

    public void ClearQueues()
    {
        _priorityQueue.Clear();
        _mainQueue.Clear();
        _lowPriorityQueue.Clear();
    }
    
    public int GetQueueCount()
    {
        return _priorityQueue.Count + _mainQueue.Count + _lowPriorityQueue.Count;
    }

    public void JoinQueue(CCSPlayerController? player, QueuePriority prio = QueuePriority.Normal)
    {
        if (player == null || !player.IsReal()) return;

        if (_priorityQueue.Contains(player) || _mainQueue.Contains(player) || _lowPriorityQueue.Contains(player)){
            IsPlayerInQueue(player, out var playerQueueStatus);
            _messageService.PrintMessage(player, _localizer["TeamEnforcer.AlreadyInQueue", playerQueueStatus?.queuePosition ?? -1, playerQueueStatus?.queueName ?? "Unknown Queue"]);
            return;
        }
        switch(prio)
        {
            case QueuePriority.High:
                _priorityQueue.Enqueue(player);
                _messageService.PrintMessage(player, _localizer["TeamEnforcer.AddedToPriorityQueue"]);
                break;
            case QueuePriority.Low:
                _lowPriorityQueue.Enqueue(player);
                _messageService.PrintMessage(player, _localizer["TeamEnforcer.AddedToLowPriorityQueue"]);
                break;
            default:
                _mainQueue.Enqueue(player);
                _messageService.PrintMessage(player, _localizer["TeamEnforcer.JoinedQueue"]);
                break;
        }
    }


    public void LeaveQueue(CCSPlayerController? player)
    {
        if (player == null || !player.IsReal()) return;

        if (!_priorityQueue.Contains(player) && !_mainQueue.Contains(player) && !_lowPriorityQueue.Contains(player)) return;

        var queues = new List<CustomQueue<CCSPlayerController>>{_priorityQueue, _mainQueue, _lowPriorityQueue};

        foreach (var queue in queues)
        {
            if (queue.Contains(player))
            {
                queue.Remove(player);
            }
        }

        _messageService.PrintMessage(player, _localizer["TeamEnforcer.RemovedFromQueue"]);
    }

    public bool IsPlayerInQueue(CCSPlayerController? player, out PlayerQueueStatus? status)
    {
        status = new PlayerQueueStatus("None", -1);

        if (player == null || !player.IsReal()) return false;

        Dictionary<string, CustomQueue<CCSPlayerController>> queues = new(){
            {"Priority Queue", _priorityQueue},
            {"Main Queue", _mainQueue},
            {"Low Priority Queue", _lowPriorityQueue}, 
        };

        var queuePos = 0;
        foreach (var queue in queues)
        {
            if (queue.Value.Contains(player))
            {
                status.queueName = queue.Key;
                status.queuePosition = queuePos + queue.Value.GetQueuePosition(player);
                return true;
            }
            queuePos += queue.Value.Count;
        }

        return false;
    }

    public List<CCSPlayerController> GetNextInQueue(int count)
    {
        List<CCSPlayerController> nextList = [];

        var queues = new List<CustomQueue<CCSPlayerController>>{_priorityQueue, _mainQueue, _lowPriorityQueue};

        foreach(var queue in queues)
        {
            while (nextList.Count < count && queue.Count > 0)
            {
                CCSPlayerController? player = queue.Dequeue();
                if (player == null || !player.IsReal() || player.Team == CsTeam.CounterTerrorist) continue;

                nextList.Add(player);
            }

            if (nextList.Count >= count) break;
        }

        return nextList;
    }
    public bool IsQueueEmpty()
    {
        if (_lowPriorityQueue.Count == 0 && _priorityQueue.Count == 0 && _mainQueue.Count == 0) return true;
        return false;
    }
    public string GetQueueStatus()
    {
        // Assumes queue isnt empty, command checks that
        var statusMessage = new StringBuilder(_localizer["TeamEnforcer.PlayersInQueueLiteral"]);
        int count = 1;
        if (_mainQueue.Count == 0 && _lowPriorityQueue.Count == 0 && _priorityQueue.Count == 0) return "";

        var allPlayers = _priorityQueue.GetAllItems()
            .Concat(_mainQueue.GetAllItems())
            .Concat(_lowPriorityQueue.GetAllItems());

        foreach (var player in allPlayers)
        {
            if (player == null || !player.IsReal()) continue;
            statusMessage.Append($"\u2029#{count} - {player.PlayerName ?? "<John Doe>"}");
            count++;
        }

        return statusMessage.ToString();
    }
}

public enum QueuePriority
{
    High,
    Normal,
    Low
}

public class PlayerQueueStatus(string name, int pos)
{
    public string queueName = name;
    public int queuePosition = pos;
}