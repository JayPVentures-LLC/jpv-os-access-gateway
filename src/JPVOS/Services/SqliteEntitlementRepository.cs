using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using JPVOS.Models;

public class SqliteEntitlementRepository : IEntitlementRepository
{
    private readonly string _dbPath;
    public SqliteEntitlementRepository(string dbPath)
    {
        _dbPath = dbPath;
        EnsureTable();
    }

    private IDbConnection GetConnection() => new SqliteConnection($"Data Source={_dbPath}");

    private void EnsureTable()
    {
        using var db = GetConnection();
        db.Execute(@"CREATE TABLE IF NOT EXISTS Entitlements (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Email TEXT,
            StripeCustomerId TEXT UNIQUE,
            StripeSubscriptionId TEXT,
            PackageKey TEXT,
            BillingInterval TEXT,
            Status TEXT,
            DiscordUserId TEXT,
            DiscordRole TEXT,
            CreatedAt TEXT,
            UpdatedAt TEXT,
            AccessExpiration TEXT
        )");
    }

    public EntitlementRecord? GetByStripeCustomerId(string customerId)
    {
        using var db = GetConnection();
        return db.QueryFirstOrDefault<EntitlementRecord>("SELECT * FROM Entitlements WHERE StripeCustomerId = @customerId", new { customerId });
    }

    public EntitlementRecord? GetByStripeSubscriptionId(string subscriptionId)
    {
        using var db = GetConnection();
        return db.QueryFirstOrDefault<EntitlementRecord>("SELECT * FROM Entitlements WHERE StripeSubscriptionId = @subscriptionId", new { subscriptionId });
    }

    public EntitlementRecord? GetByDiscordUserId(string discordUserId)
    {
        using var db = GetConnection();
        return db.QueryFirstOrDefault<EntitlementRecord>("SELECT * FROM Entitlements WHERE DiscordUserId = @discordUserId", new { discordUserId });
    }

    public void AddOrUpdate(EntitlementRecord record)
    {
        using var db = GetConnection();
        var existing = GetByStripeSubscriptionId(record.StripeSubscriptionId);
        if (existing != null)
        {
            // Idempotency: update existing
            db.Execute(@"UPDATE Entitlements SET
                Email = @Email,
                StripeCustomerId = @StripeCustomerId,
                PackageKey = @PackageKey,
                BillingInterval = @BillingInterval,
                Status = @Status,
                DiscordUserId = @DiscordUserId,
                DiscordRole = @DiscordRole,
                UpdatedAt = @UpdatedAt,
                AccessExpiration = @AccessExpiration
                WHERE StripeSubscriptionId = @StripeSubscriptionId",
                record);
        }
        else
        {
            db.Execute(@"INSERT INTO Entitlements (
                Email, StripeCustomerId, StripeSubscriptionId, PackageKey, BillingInterval, Status, DiscordUserId, DiscordRole, CreatedAt, UpdatedAt, AccessExpiration
            ) VALUES (
                @Email, @StripeCustomerId, @StripeSubscriptionId, @PackageKey, @BillingInterval, @Status, @DiscordUserId, @DiscordRole, @CreatedAt, @UpdatedAt, @AccessExpiration
            )", record);
        }
    }

    public void RemoveByStripeCustomerId(string customerId)
    {
        using var db = GetConnection();
        db.Execute("DELETE FROM Entitlements WHERE StripeCustomerId = @customerId", new { customerId });
    }

    public void RemoveByStripeSubscriptionId(string subscriptionId)
    {
        using var db = GetConnection();
        db.Execute("DELETE FROM Entitlements WHERE StripeSubscriptionId = @subscriptionId", new { subscriptionId });
    }

    public IEnumerable<EntitlementRecord> GetAll()
    {
        using var db = GetConnection();
        return db.Query<EntitlementRecord>("SELECT * FROM Entitlements");
    }
}
