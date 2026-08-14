using System.Text.Json.Serialization;

namespace JPVOS.Services.SystemicAccess;

public sealed class SystemicAccessPolicy
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("actions")]
    public string[] Actions { get; init; } = [];

    [JsonPropertyName("default_uncertain_action")]
    public string DefaultUncertainAction { get; init; } = string.Empty;

    [JsonPropertyName("re_enable_requires")]
    public string ReEnableRequires { get; init; } = string.Empty;

    [JsonPropertyName("preserve_audit_receipt")]
    public bool PreserveAuditReceipt { get; init; }

    [JsonPropertyName("interpretation")]
    public SystemicAccessInterpretation Interpretation { get; init; } = new();
}

public sealed class SystemicAccessInterpretation
{
    [JsonPropertyName("systemic_only")]
    public bool SystemicOnly { get; init; }

    [JsonPropertyName("person_targeting")]
    public bool PersonTargeting { get; init; }
}
