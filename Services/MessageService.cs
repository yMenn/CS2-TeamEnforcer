using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using TeamEnforcer.Helpers;
using System.Text.RegularExpressions;

namespace TeamEnforcer.Services;

public class MessageService(string pluginPrefix = "[TeamEnforcer]")
{
    public Dictionary<MsgType, char> messageColors = new()
    {
        { MsgType.Normal, ChatColors.Default},
        { MsgType.Warning, ChatColors.Yellow},
        { MsgType.Error, ChatColors.Red }
    };
    
    public string Prefix { get; set; } = pluginPrefix;

    public void PrintToConsole(string message)
    {
        var fullMessage = new StringBuilder("[TeamEnforcer] ").Append(message);
        Console.WriteLine(fullMessage.ToString());
    }

    public void PrintMessage(CCSPlayerController? player, string message, MsgType type = MsgType.Normal)
    {
        if (player == null || !player.IsReal()) return;
        
        var fullMessage = new StringBuilder(ReplaceTags(Prefix))
            .Append($" {messageColors.GetValueOrDefault(type, ChatColors.Default)}")
            .Append(ReplaceTags(message))
            .Append($"{ChatColors.Default}");

        player.PrintToChat(fullMessage.ToString());
    }

    public string GetMessageString(string message, MsgType type = MsgType.Normal)
    {
        var fullMessage = new StringBuilder(ReplaceTags(Prefix))
            .Append($" {messageColors.GetValueOrDefault(type, ChatColors.Default)}")
            .Append(ReplaceTags(message))
            .Append($"{ChatColors.Default}");

        return fullMessage.ToString();
    }

    public void PrintToAll(string message, MsgType type = MsgType.Normal)
    {
        var fullMessage = new StringBuilder(ReplaceTags(Prefix))
            .Append($" {messageColors.GetValueOrDefault(type, ChatColors.Default)}")
            .Append(ReplaceTags(message))
            .Append($"{ChatColors.Default}") ;

        Server.PrintToChatAll(fullMessage.ToString());
    }
    public string ReplaceTags(string text)
    {
        text = Regex.Replace(text, "{DEFAULT}", $"{ChatColors.Default}", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{WHITE}", $"{ChatColors.White}", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{DARKRED}", $"{ChatColors.DarkRed}", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{GREEN}", $"{ChatColors.Green}", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{LIGHTYELLOW}", $"{ChatColors.LightYellow}", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{LIGHTBLUE}", $"{ChatColors.LightBlue}", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{OLIVE}", $"{ChatColors.Olive}", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{LIME}", $"{ChatColors.Lime}", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{RED}", $"{ChatColors.Red}", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{PURPLE}", $"{ChatColors.Purple}", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{GREY}", $"{ChatColors.Grey}", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{YELLOW}", $"{ChatColors.Yellow}", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{GOLD}", $"{ChatColors.Gold}", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{SILVER}", $"{ChatColors.Silver}", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{BLUE}", $"{ChatColors.Blue}", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{DARKBLUE}", $"{ChatColors.DarkBlue}", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{BLUEGREY}", $"{ChatColors.BlueGrey}", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{MAGENTA}", $"{ChatColors.Magenta}", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{LIGHTRED}", $"{ChatColors.LightRed}", RegexOptions.IgnoreCase);

	    return text;
    }
}

public enum MsgType
{
    Normal,
    Warning,
    Error
} 