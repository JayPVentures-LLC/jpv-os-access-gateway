using Microsoft.Data.Sqlite;

namespace JPVOS.Services.ProposalExecution;

public sealed class SqliteProposalEventStore : IProposalEventStore
{
    private readonly string _connectionString;

    public SqliteProposalEventStore(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWriteCreate }.ToString();
        Initialize();
    }

    private void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS proposal_events (
              event_id TEXT PRIMARY KEY,
              proposal_id TEXT NOT NULL,
              sequence INTEGER NOT NULL,
              occurred_at_utc TEXT NOT NULL,
              event_type TEXT NOT NULL,
              idempotency_key TEXT NULL,
              payload_json TEXT NOT NULL,
              UNIQUE(proposal_id, sequence)
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ux_proposal_event_key
              ON proposal_events(proposal_id, idempotency_key)
              WHERE idempotency_key IS NOT NULL;
            """;
        command.ExecuteNonQuery();
    }

    public async Task AppendAsync(ProposalLifecycleEvent item, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            if (!string.IsNullOrWhiteSpace(item.IdempotencyKey))
            {
                await using var existing = connection.CreateCommand();
                existing.Transaction = transaction;
                existing.CommandText = "SELECT 1 FROM proposal_events WHERE proposal_id=$proposal AND idempotency_key=$key LIMIT 1";
                existing.Parameters.AddWithValue("$proposal", item.ProposalId);
                existing.Parameters.AddWithValue("$key", item.IdempotencyKey);
                if (await existing.ExecuteScalarAsync(cancellationToken) is not null)
                {
                    transaction.Commit();
                    return;
                }
            }

            await using var sequenceCommand = connection.CreateCommand();
            sequenceCommand.Transaction = transaction;
            sequenceCommand.CommandText = "SELECT COALESCE(MAX(sequence),0)+1 FROM proposal_events WHERE proposal_id=$proposal";
            sequenceCommand.Parameters.AddWithValue("$proposal", item.ProposalId);
            var sequence = Convert.ToInt64(await sequenceCommand.ExecuteScalarAsync(cancellationToken));

            await using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = "INSERT INTO proposal_events(event_id,proposal_id,sequence,occurred_at_utc,event_type,idempotency_key,payload_json) VALUES($id,$proposal,$sequence,$occurred,$type,$key,$payload)";
            insert.Parameters.AddWithValue("$id", item.EventId);
            insert.Parameters.AddWithValue("$proposal", item.ProposalId);
            insert.Parameters.AddWithValue("$sequence", sequence);
            insert.Parameters.AddWithValue("$occurred", item.OccurredAtUtc.ToUniversalTime().ToString("O"));
            insert.Parameters.AddWithValue("$type", item.Type.ToString());
            insert.Parameters.AddWithValue("$key", (object?)item.IdempotencyKey ?? DBNull.Value);
            insert.Parameters.AddWithValue("$payload", item.PayloadJson);
            await insert.ExecuteNonQueryAsync(cancellationToken);
            transaction.Commit();
        }
        catch (OperationCanceledException)
        {
            transaction.Rollback();
            throw;
        }
        catch (SqliteException ex)
        {
            transaction.Rollback();
            throw new ProposalPersistenceException("Failed to append proposal lifecycle event.", ex);
        }
    }

    public async Task<IReadOnlyList<ProposalLifecycleEvent>> ReadStreamAsync(string proposalId, CancellationToken cancellationToken)
    {
        var events = new List<ProposalLifecycleEvent>();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT event_id,proposal_id,sequence,occurred_at_utc,event_type,idempotency_key,payload_json FROM proposal_events WHERE proposal_id=$proposal ORDER BY sequence";
        command.Parameters.AddWithValue("$proposal", proposalId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            events.Add(new ProposalLifecycleEvent(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt64(2),
                DateTime.Parse(reader.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind),
                Enum.Parse<ProposalEventType>(reader.GetString(4), true),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetString(6)));
        }
        return events;
    }
}
