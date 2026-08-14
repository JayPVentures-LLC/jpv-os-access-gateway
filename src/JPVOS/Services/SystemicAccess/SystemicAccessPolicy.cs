using System.Text.Json.Serialization;

namespace JPVOS.Services.SystemicAccess;

public sealed class SystemicAccessPolicy
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("canonical_source")]
    public SystemicAccessCanonicalSource CanonicalSource { get; init; } = new();

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

public sealed class SystemicAccessCanonicalSource
{
    [JsonPropertyName("repository")]
    public string Repository { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("baseline_commit")]
    public string BaselineCommit { get; init; } = string.Empty;
}

public sealed class SystemicAccessInterpretation
{
    [JsonPropertyName("systemic_only")]
    public bool SystemicOnly { get; init; }

    [JsonPropertyName("person_targeting")]
    public bool PersonTargeting { get; init; }
}
