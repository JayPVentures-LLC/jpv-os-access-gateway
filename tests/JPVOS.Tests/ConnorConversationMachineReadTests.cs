using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JPVOS.Services.Outbound;

namespace JPVOS.Tests;

public sealed class ConnorConversationMachineReadTests
{
    [Fact]
    public void MachineReadTokenRequiresExactBearerSecretAndConfiguredDigest()
    {
        const string token = "unit-test-connor-read-secret";
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        Assert.True(MachineReadTokenAuthenticator.IsAuthorized($"Bearer {token}", digest));
        Assert.False(MachineReadTokenAuthenticator.IsAuthorized("Bearer wrong", digest));
        Assert.False(MachineReadTokenAuthenticator.IsAuthorized(token, digest));
        Assert.False(MachineReadTokenAuthenticator.IsAuthorized($"Bearer {token}", null));
        Assert.False(MachineReadTokenAuthenticator.IsAuthorized($"Bearer {token}", "not-hex"));
    }

    [Fact]
    public async Task ProjectionIsFixedToConnorAndDoesNotExposeProviderOrEndpointMetadata()
    {
        var store = new InMemoryDirectConversationStore();
        await store.SaveAsync(new ConversationMessage(
            "out-1",
            DirectConversationService.ConnorConversationId,
            PrincipalSmsBindingResolver.ConnorPrincipalId,
            ConversationDirection.Outbound,
            "hello",
            DateTimeOffset.Parse("2026-09-12T20:00:00Z"),
            "SM-provider-secret-id",
            OutboundMessageState.Delivered), CancellationToken.None);
        await store.TryInsertInboundIfNewAsync(new ConversationMessage(
            "in-1",
            DirectConversationService.ConnorConversationId,
            PrincipalSmsBindingResolver.ConnorPrincipalId,
            ConversationDirection.Inbound,
            "ma'am",
            DateTimeOffset.Parse("2026-09-12T20:01:00Z"),
            "SM-provider-inbound-id",
            OutboundMessageState.Delivered), CancellationToken.None);

        var result = await new ConnorConversationProjectionService(store).ReadAsync(CancellationToken.None);

        Assert.Equal("jpv.connor-direct-conversation-read.v1", result.SchemaVersion);
        Assert.Equal(PrincipalSmsBindingResolver.ConnorPrincipalId, result.SubjectId);
        Assert.Equal(DirectConversationService.ConnorConversationId, result.ConversationId);
        Assert.True(result.ReadOnly);
        Assert.Collection(result.Messages,
            first => { Assert.Equal("outbound", first.Direction); Assert.Equal("hello", first.Body); },
            second => { Assert.Equal("inbound", second.Direction); Assert.Equal("ma'am", second.Body); });

        var json = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("SM-provider", json, StringComparison.Ordinal);
        Assert.DoesNotContain("providerMessageId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("endpoint", json, StringComparison.OrdinalIgnoreCase);
    }
}
