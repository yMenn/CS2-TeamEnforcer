# CS2-TeamEnforcer Plugin

**TeamEnforcer** is a Counter-Strike 2 plugin designed to manage and balance teams in the Jailbreak gamemode. This plugin provides a queue system for joining the CT team, enforces team balance, and includes features like CT bans and admin controls. This plugin is in it's early stages and you must understand this is only my second plugin therefore it is not perfect and might have several bugs/ inefficiencies. I am always looking for suggestions and feedback to improve the plugin and will update it as I learn more about plugin creation.

## 🎯 Features

- **Queue System**: Players must use a queue to join the CT team, preventing direct team switches. Commands available for joining, leaving, and viewing the queue.
- **Team Balance**: Automatically balances teams based on a configurable CT ratio. Includes automatic promotion from the queue and random selection when necessary.
- **CT Ban and Kick System**: Admins can ban players from joining CT for a specified duration or permanently, kick players from CT, view ban information, and unban players.
- **Multi-tier Queue Priority**: 
  - Players removed from CT due to overstaffing get high priority in the queue.
  - Players who were CT for a configurable minimum number of rounds in the previous map get low priority.
- **No CT List**: Players can opt out of being randomly selected for CT.
- **Illegitimate CT Detection**: Automatically demotes players who joined CT through unauthorized means in the case of unforseen conditions where players are able to join to the CT team.
- **Admin Commands**: Various admin commands for managing teams and players.
- **Localization Support**: Supports multiple languages for server messages.
- **Database Integration**: Uses MySQL for storing CT ban information.

## ⚙️ Commands

Here's a list of available commands:

| Command | Description |
|---------|-------------|
| `!ct` or `!guard` | Join the CT queue |
| `!t` | Leave the CT team (for CTs) |
| `!noct` | Add yourself to the no-CT list (won't be randomly selected for CT) |
| `!lq` or `!leavequeue` | Leave the CT queue |
| `!vq` or `!queue` | View the current queue status |

### 🛡️ Admin Commands

| Command | Description |
|---------|-------------|
| `!ctban <player> [duration] [reason]` | Ban a player from joining CT |
| `!ctunban <player> [reason]` | Unban a player from CT |
| `!ctbaninfo <player>` | View CT ban information for a player |
| `!forcect <player>` | Force a player to join the CT team |
| `!ctkick <player>` | Kick a player from the CT team |
| `!removequeue <player>` | Remove a player from the CT queue |

## 🛣️ ROADMAP

Here are some planned features and improvements for the TeamEnforcer plugin:

- **Performance Optimizations**: Implement caching for CT bans to reduce database operations.
- **Command Cooldowns**: Add cooldowns to player commands to prevent spam.
- **WASD Menu**: Implement a user-friendly WASD-based menu for admins to manage plugin features.
- **Extended Logging**: Enhance logging capabilities for better server management and troubleshooting.
- **Integration with Other Plugins**: Explore possibilities to integrate with other popular CS2 plugins.

## 🚀 How to Install

1. Download the latest release from the releases section.
2. Place the plugin folder in `csgo/addons/counterstrikesharp/plugins`
3. After first startup, configure the plugin in `csgo/addons/counterstrikesharp/configs/plugins/TeamEnforcer/TeamEnforcer.json`
4. Set up your MySQL database and update the connection details in the config file.
5. Restart your server or reload the plugin to apply the changes.

## ⚙️ Configuration

The plugin uses a JSON configuration file. Here are the main settings:

```json
{
    "ChatMessagePrefix": " {DarkBlue}[{LightBlue}TeamEnforcer{DarkBlue}]{Default}",
    "RoundsInCtToLowPrio": 2,
    "DefaultCTRatio": 0.25,
    "DatabaseHost": "",
    "DatabasePort": 3306,
    "DatabaseUser": "",
    "DatabasePassword": "",
    "DatabaseName": ""
}
```

Adjust these settings according to your server's needs.

## 💬 Feedback and Contributions

Your feedback is crucial and appreciated for improving TeamEnforcer! If you encounter any issues, have feature requests, or want to contribute, please feel free to open an issue or pull request on our GitHub repository. Altough I have some experience with github I may need to figure some things out so please be patient with me.

## 🌐 Localization

TeamEnforcer currently supports english and portuguese. Any contributions to add more languages are welcome.

## 📜 License

This project is licensed under the MIT license. See the LICENSE file for more details.

---

Thank you for using TeamEnforcer! I hope this makes your Jailbreak server more enjoyable.
