using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using TeamEnforcer.Helpers;

namespace TeamEnforcer.Services;

public class CTBanService(Database db, ILogger logger)
{
    private readonly Database _db = db;
    private readonly ILogger _logger = logger;

    public void CreateTables()
    {
        using var connection = _db.GetConnection();

        // Create ban table
        using var banTableCommand = new MySqlCommand("""
        CREATE TABLE IF NOT EXISTS `teamenforcer_ctbans` 
        (
            `id` INT PRIMARY KEY AUTO_INCREMENT,
            `player_steamid` VARCHAR(32) NOT NULL,
            `staff_steamid` VARCHAR(32) NOT NULL,
            `ban_reason` VARCHAR(255) NOT NULL,
            `ban_date` DATETIME NOT NULL,
            `expiration_date` DATETIME NULL,
            `active` BOOL NOT NULL
        );
        """, connection);

        banTableCommand.ExecuteNonQuery();

        // Create unban table with foreign key reference to ban table
        using var unbanTableCommand = new MySqlCommand("""
        CREATE TABLE IF NOT EXISTS `teamenforcer_ctunbans` 
        (
            `id` INT PRIMARY KEY AUTO_INCREMENT,
            `ban_id` INT NOT NULL,
            `staff_steamid` VARCHAR(32) NOT NULL,
            `unban_reason` VARCHAR(255) NOT NULL,
            `unban_date` DATETIME NOT NULL,
            CONSTRAINT fk_ban
                FOREIGN KEY (`ban_id`) REFERENCES `teamenforcer_ctbans`(`id`)
                ON DELETE CASCADE
        );
        """, connection);

        unbanTableCommand.ExecuteNonQuery();
    }

    public async Task<ExistingCTBan> BanPlayerAsync(NewCTBan newCtban)
    {
        using var connection = await _db.GetConnectionAsync();
        using var command = new MySqlCommand("""
        INSERT INTO `teamenforcer_ctbans` (player_steamid, staff_steamid, ban_reason, ban_date, expiration_date, active)
        VALUES (@playerSteamId, @staffSteamId, @banReason, @banDate, @expirationDate, @active);
        SELECT LAST_INSERT_ID();
        """, connection);

        command.Parameters.AddWithValue("@playerSteamId", newCtban.PlayerSteamId);
        command.Parameters.AddWithValue("@staffSteamId", newCtban.StaffSteamId);
        command.Parameters.AddWithValue("@banReason", newCtban.BanReason);
        command.Parameters.AddWithValue("@banDate", newCtban.BanDate);
        command.Parameters.AddWithValue("@expirationDate", newCtban.ExpirationDate);
        command.Parameters.AddWithValue("@active", newCtban.Active);

        var id = Convert.ToInt32(await command.ExecuteScalarAsync());

        _logger.LogInformation("[TeamEnforcer] Player {PlayerSteamId} CTBanned by {StaffSteamId}. Ban ID: {BanId}, Reason: {BanReason}, Expiration: {ExpirationDate}", 
            newCtban.PlayerSteamId, newCtban.StaffSteamId, id, newCtban.BanReason, newCtban.ExpirationDate?.ToString() ?? "Never");

        return new ExistingCTBan
        {
            Id = id,
            PlayerSteamId = newCtban.PlayerSteamId,
            StaffSteamId = newCtban.StaffSteamId,
            BanReason = newCtban.BanReason,
            BanDate = newCtban.BanDate,
            ExpirationDate = newCtban.ExpirationDate,
            Active = newCtban.Active
        };
    }

    public async Task<CTUnban> UnbanPlayerAsync(ExistingCTBan ctban, string staffSteamId, string unbanReason)
    {
        using var connection = await _db.GetConnectionAsync();
        using var command = new MySqlCommand("""
        UPDATE `teamenforcer_ctbans` 
        SET `active` = FALSE 
        WHERE `id` = @banId;
        
        INSERT INTO `teamenforcer_ctunbans` (ban_id, staff_steamid, unban_reason, unban_date)
        VALUES (@banId, @staffSteamId, @unbanReason, @unbanDate);
        """, connection);

        command.Parameters.AddWithValue("@banId", ctban.Id);
        command.Parameters.AddWithValue("@staffSteamId", staffSteamId);
        command.Parameters.AddWithValue("@unbanReason", unbanReason);
        command.Parameters.AddWithValue("@unbanDate", DateTime.Now);

        await command.ExecuteNonQueryAsync();
        _logger.LogInformation("[TeamEnforcer] Player with ban ID {BanId} CTUnbanned by {StaffSteamId}. Reason: {UnbanReason}", 
            ctban.Id, staffSteamId, unbanReason);
        return new CTUnban
        {
            BanId = ctban.Id,
            StaffSteamId = staffSteamId,
            UnbanReason = unbanReason,
            UnbanDate = DateTime.Now
        };
    }

    public async Task<ExistingCTBan?> GetCTBanInfoAsync(CCSPlayerController? player)
    {
        if (player == null || !player.IsReal()) return null;

        using var connection = await _db.GetConnectionAsync();
        using var command = new MySqlCommand("""
        SELECT `id`, `player_steamid`, `staff_steamid`, `ban_reason`, `ban_date`, `expiration_date`, `active`
        FROM `teamenforcer_ctbans`
        WHERE `player_steamid` = @playerSteamId AND `active` = TRUE;
        """, connection);

        await Server.NextFrameAsync(() => {
            command.Parameters.AddWithValue("@playerSteamId", player.SteamID);
        });

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var expirationDateOrdinal = reader.GetOrdinal("expiration_date");
            return new ExistingCTBan
            {
                Id = reader.GetInt32("id"),
                PlayerSteamId = reader.GetString("player_steamid"),
                StaffSteamId = reader.GetString("staff_steamid"),
                BanReason = reader.GetString("ban_reason"),
                BanDate = reader.GetDateTime("ban_date"),
                ExpirationDate = reader.IsDBNull(expirationDateOrdinal) ? (DateTime?)null : reader.GetDateTime(expirationDateOrdinal),
                Active = reader.GetBoolean("active")
            };
        }

        return null;
    }

    public async Task<bool> PlayerIsCTBannedAsync(CCSPlayerController? player)
    {
        if (player == null || !player.IsReal()) return false;

        using var connection = await _db.GetConnectionAsync();
        
        using var command = new MySqlCommand("""
        SELECT `id`, `expiration_date`, `active`
        FROM `teamenforcer_ctbans`
        WHERE `player_steamid` = @playerSteamId AND `active` = TRUE;
        """, connection);

        await Server.NextFrameAsync(() => {
            command.Parameters.AddWithValue("@playerSteamId", player.SteamID);
        });

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            int banId = reader.GetInt32("id");
            var expirationDateOrdinal = reader.GetOrdinal("expiration_date");

            DateTime? expirationDate = reader.IsDBNull(expirationDateOrdinal) ? (DateTime?)null : reader.GetDateTime(expirationDateOrdinal);
            bool isActive = reader.GetBoolean("active");

            await reader.CloseAsync();

            if (expirationDate.HasValue && expirationDate.Value <= DateTime.Now)
            {
                await MarkBanAsExpiredAsync(banId);
                return false;
            }

            return isActive;
        }

        return false;
    }

    public bool PlayerIsCTBanned(CCSPlayerController? player)
    {
        if (player == null || !player.IsReal()) return false;

        using var connection = _db.GetConnection();
        
        using var command = new MySqlCommand("""
        SELECT `id`, `expiration_date`, `active`
        FROM `teamenforcer_ctbans`
        WHERE `player_steamid` = @playerSteamId AND `active` = TRUE;
        """, connection);

        command.Parameters.AddWithValue("@playerSteamId", player.SteamID);

        using var reader = command.ExecuteReader();

        if (reader.Read())
        {
            int banId = reader.GetInt32("id");
            var expirationDateOrdinal = reader.GetOrdinal("expiration_date");

            DateTime? expirationDate = reader.IsDBNull(expirationDateOrdinal) ? (DateTime?)null : reader.GetDateTime(expirationDateOrdinal);
            bool isActive = reader.GetBoolean("active");

            reader.Close();

            if (expirationDate.HasValue && expirationDate.Value <= DateTime.Now)
            {
                Task.Run(() => MarkBanAsExpiredAsync(banId));
                return false;
            }

            return isActive;
        }
        return false;
    }

    private async Task MarkBanAsExpiredAsync(int banId)
    {
        using var connection = await _db.GetConnectionAsync();
        
        using var updateCommand = new MySqlCommand("""
        UPDATE `teamenforcer_ctbans`
        SET `active` = FALSE
        WHERE `id` = @banId;
        """, connection);

        updateCommand.Parameters.AddWithValue("@banId", banId);

        await updateCommand.ExecuteNonQueryAsync();
    }

}

public class NewCTBan
{
    public required string PlayerSteamId { get; set; }
    public required string StaffSteamId { get; set; }
    public required string BanReason { get; set; }
    public DateTime BanDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool Active { get; set; }
}

public class ExistingCTBan : NewCTBan
{
    public int Id { get; set; }
}

public class CTUnban
{
    public int Id { get; set; }
    public int BanId { get; set; }
    public required string StaffSteamId { get; set; }
    public required string UnbanReason { get; set; }
    public DateTime UnbanDate { get; set; }
}

public enum BanRemoveReason
{
    Expired,
    StaffRevoke
}