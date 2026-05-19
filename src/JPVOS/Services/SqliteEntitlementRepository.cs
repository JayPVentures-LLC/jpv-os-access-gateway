using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using JPVOS.Models;
using Microsoft.Extensions.Logging;

public class SqliteEntitlementRepository : IEntitlementRepository
{
    private readonly string _dbPath;
    private readonly ILogger<SqliteEntitlementRepository>? _logger;

    public SqliteEntitlementRepository(string dbPath, ILogger<SqliteEntitlementRepository>? logger = null)
    {
        _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
        _logger = logger;
        
        try
        {
            ValidateAndInitializeDatabase();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize entitlements database at {DbPath}", _dbPath);
            throw;
        }
    }

    private void ValidateAndInitializeDatabase()
    {
        var directory = Path.GetDirectoryName(_dbPath);
        
        // Ensure directory exists
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            try
            {
                Directory.CreateDirectory(directory);
                _logger?.LogInformation("Created entitlements database directory: {Directory}", directory);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create database directory: {Directory}", directory);
                throw;
            }
        }

        // Validate write and read permissions
        if (File.Exists(_dbPath))
        {
            try
            {
                // Test read permission
                using (var fs = File.OpenRead(_dbPath))
                {
                }
                // Test write permission by opening for read/write
                using (var fs = File.Open(_dbPath, FileMode.Open, FileAccess.ReadWrite))
                {
                }
                _logger?.LogInformation("Entitlements database accessible at {DbPath}", _dbPath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Insufficient permissions to access entitlements database at {DbPath}", _dbPath);
                throw;
            }
        }
        else
        {
            try
            {
                // Test write permission by creating and removing a test file in the directory
                var testFile = Path.Combine(directory ?? AppContext.BaseDirectory, ".write-test-" + Guid.NewGuid());
                using (var fs = File.Create(testFile))
                {
                }
                File.Delete(testFile);
                
                // Create the database file
                using (var fs = File.Create(_dbPath))
                {
                }
                _logger?.LogInformation("Created new entitlements database at {DbPath}", _dbPath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create entitlements database at {DbPath}. Check directory write permissions.", _dbPath);
                throw;
            }
        }

        EnsureTable();
    }

    private IDbConnection GetConnection() => new SqliteConnection($"Data Source={_dbPath}");

    private void EnsureTable()
    {
        try
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
            _logger?.LogInformation("Entitlements table ensured");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to ensure Entitlements table exists");
            throw;
        }
    }

    public EntitlementRecord? GetByStripeCustomerId(string customerId)
    {
        try
        {
            using var db = GetConnection();
            return db.QueryFirstOrDefault<EntitlementRecord>("SELECT * FROM Entitlements WHERE StripeCustomerId = @customerId", new { customerId });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get entitlement by Stripe customer ID: {CustomerId}", customerId);
            throw;
        }
    }

    public EntitlementRecord? GetByStripeSubscriptionId(string subscriptionId)
    {
        try
        {
            using var db = GetConnection();
            return db.QueryFirstOrDefault<EntitlementRecord>("SELECT * FROM Entitlements WHERE StripeSubscriptionId = @subscriptionId", new { subscriptionId });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get entitlement by Stripe subscription ID: {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public EntitlementRecord? GetByDiscordUserId(string discordUserId)
    {
        try
        {
            using var db = GetConnection();
            return db.QueryFirstOrDefault<EntitlementRecord>("SELECT * FROM Entitlements WHERE DiscordUserId = @discordUserId", new { discordUserId });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get entitlement by Discord user ID: {DiscordUserId}", discordUserId);
            throw;
        }
    }

    public void AddOrUpdate(EntitlementRecord record)
    {
        try
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
                _logger?.LogInformation("Updated entitlement for Stripe subscription: {SubscriptionIdHash}", 
                    ComputeHash(record.StripeSubscriptionId));
            }
            else
            {
                db.Execute(@"INSERT INTO Entitlements (
                    Email, StripeCustomerId, StripeSubscriptionId, PackageKey, BillingInterval, Status, DiscordUserId, DiscordRole, CreatedAt, UpdatedAt, AccessExpiration
                ) VALUES (
                    @Email, @StripeCustomerId, @StripeSubscriptionId, @PackageKey, @BillingInterval, @Status, @DiscordUserId, @DiscordRole, @CreatedAt, @UpdatedAt, @AccessExpiration
                )", record);
                _logger?.LogInformation("Created new entitlement for Stripe subscription: {SubscriptionIdHash}", 
                    ComputeHash(record.StripeSubscriptionId));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to add or update entitlement for customer: {CustomerIdHash}", 
                ComputeHash(record.StripeCustomerId));
            throw;
        }
    }

    private static string ComputeHash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "[empty]";
        // Log first few characters + hash for debugging without exposing full PII
        var prefix = input.Length > 8 ? input.Substring(0, 8) : input;
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        var hashHex = System.Convert.ToHexString(hash).Substring(0, 8);
        return $"{prefix}...{hashHex}";
    }

    public void RemoveByStripeCustomerId(string customerId)
    {
        try
        {
            using var db = GetConnection();
            var count = db.Execute("DELETE FROM Entitlements WHERE StripeCustomerId = @customerId", new { customerId });
            if (count > 0)
            {
                _logger?.LogInformation("Removed entitlement for Stripe customer: {CustomerId}", customerId);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to remove entitlement by Stripe customer ID: {CustomerId}", customerId);
            throw;
        }
    }

    public void RemoveByStripeSubscriptionId(string subscriptionId)
    {
        try
        {
            using var db = GetConnection();
            var count = db.Execute("DELETE FROM Entitlements WHERE StripeSubscriptionId = @subscriptionId", new { subscriptionId });
            if (count > 0)
            {
                _logger?.LogInformation("Removed entitlement for Stripe subscription: {SubscriptionId}", subscriptionId);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to remove entitlement by Stripe subscription ID: {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public IEnumerable<EntitlementRecord> GetAll()
    {
        try
        {
            using var db = GetConnection();
            return db.Query<EntitlementRecord>("SELECT * FROM Entitlements");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get all entitlements");
            throw;
        }
    }
}

