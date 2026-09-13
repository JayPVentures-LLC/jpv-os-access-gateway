using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace JPVOS.Services.Outbound;

public static class MachineReadTokenAuthenticator
{
    public static bool IsAuthorized(string? authorizationHeader, string? expectedSha256Hex)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader) || string.IsNullOrWhiteSpace(expectedSha256Hex)) return false;
        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return false;
        var token = authorizationHeader[7..].Trim();
        if (token.Length == 0) return false;
        try
        {
            var expected = Convert.FromHexString(expectedSha256Hex.Trim());
            var actual = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed record ConnorConversationReadMessage(
    [property: JsonPropertyName("message_id")] string MessageId,
    [property: JsonPropertyName("direction")] string Direction,
    [property: JsonPropertyName("body")] string Body,
    [property: JsonPropertyName("created_at_utc")] DateTimeOffset CreatedAtUtc,
    [property: JsonPropertyName("delivery_state")] string? DeliveryState);

public sealed record ConnorConversationReadEnvelope(
    [property: JsonPropertyName("schema_version")] string SchemaVersion,
    [property: JsonPropertyName("subject_id")] string SubjectId,
    [property: JsonPropertyName("conversation_id")] string ConversationId,
    [property: JsonPropertyName("read_only")] bool ReadOnly,
    [property: JsonPropertyName("messages")] IReadOnlyList<ConnorConversationReadMessage> Messages);

public sealed class ConnorConversationProjectionService
{
    private const int MaxMessages = 500;
    private readonly IDirectConversationStore _store;

    public ConnorConversationProjectionService(IDirectConversationStore store) => _store = store;

    public async Task<ConnorConversationReadEnvelope> ReadAsync(CancellationToken cancellationToken)
    {
        var transcript = await _store.GetConversationAsync(DirectConversationService.ConnorConversationId, cancellationToken);
        var messages = transcript
            .Where(message => string.Equals(message.PrincipalId, PrincipalSmsBindingResolver.ConnorPrincipalId, StringComparison.Ordinal))
            .OrderBy(message => message.CreatedAtUtc)
            .TakeLast(MaxMessages)
            .Select(message => new ConnorConversationReadMessage(
                message.MessageId,
                message.Direction == ConversationDirection.Inbound ? "inbound" : "outbound",
                message.Body,
                message.CreatedAtUtc,
                message.DeliveryState?.ToString().ToUpperInvariant()))
            .ToArray();

        return new ConnorConversationReadEnvelope(
            "jpv.connor-direct-conversation-read.v1",
            PrincipalSmsBindingResolver.ConnorPrincipalId,
            DirectConversationService.ConnorConversationId,
            true,
            messages);
    }
}
