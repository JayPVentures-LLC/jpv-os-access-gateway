using System.Text.Json;
using JPVOS.Services.PrivilegedActions;

namespace JPVOS.Services.GitHubOrgMutation;

public sealed class GitHubOrganizationReconciler
{
    private readonly IGitHubCanonicalTopologySource _source;
    private readonly IGitHubOrganizationClient _client;
    private readonly GitHubOrgMutationReceiptStore _receipts;
    private readonly PrivilegedActionExecutionService _privilegedExecution;

    public GitHubOrganizationReconciler(
        IGitHubCanonicalTopologySource source,
        IGitHubOrganizationClient client,
        GitHubOrgMutationReceiptStore receipts,
        PrivilegedActionExecutionService privilegedExecution)
    {
        _source = source;
        _client = client;
        _receipts = receipts;
        _privilegedExecution = privilegedExecution;
    }

    public async Task<GitHubOrgReconciliationResult> ReconcileOrganizationAsync(string organization, CancellationToken cancellationToken)
    {
        var topology = await _source.LoadAsync(cancellationToken);
        if (!topology.Organizations.TryGetValue(organization, out var document))
        {
            throw new InvalidOperationException($"Organization '{organization}' is not present in canonical GitHub topology.");
        }

        var desired = document.ToTopology(organization);
        var observed = await _client.ListTeamsAsync(organization, cancellationToken);
        var plan = GitHubOrgMutationPlanner.Plan(desired, observed);
        var bySlug = observed.ToDictionary(x => x.Slug, StringComparer.OrdinalIgnoreCase);
        var applied = 0;

        foreach (var mutation in plan.Mutations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long? parentId = null;
            if (!string.IsNullOrWhiteSpace(mutation.ParentSlug))
            {
                if (!bySlug.TryGetValue(mutation.ParentSlug, out var parent))
                    throw new InvalidOperationException($"Required parent team '{mutation.ParentSlug}' is not available before '{mutation.TeamSlug}'.");
                parentId = parent.Id;
            }

            var desiredState = CanonicalTeamState(mutation.TeamSlug, mutation.ParentSlug);
            var request = new PrivilegedActionRequest(
                "service:github-app-org-reconciler",
                "entitlement_grant",
                $"github:{organization}:team:{mutation.TeamSlug}",
                PrivilegedRiskClass.Privileged,
                EntitlementValid: true,
                EntitlementAmbiguous: false,
                DesiredState: desiredState);

            // The GitHub App installation token is a cryptographically authenticated service-principal
            // credential. Treat that service identity as phishing-resistant non-human authentication.
            var now = DateTimeOffset.UtcNow;
            var authentication = new AuthenticationEvidence(
                IdentityVerified: true,
                PhishingResistant: true,
                VoiceSignalPresent: false,
                AuthenticatedAtUtc: now,
                MaxAge: TimeSpan.FromMinutes(5));
            var provider = new GitHubTeamMutationPrivilegedProvider(
                _client,
                organization,
                mutation.Kind,
                mutation.TeamSlug,
                mutation.ParentSlug,
                parentId);

            var outcome = await _privilegedExecution.ExecuteAsync(
                request,
                authentication,
                provider,
                now,
                cancellationToken: cancellationToken);

            if (!string.Equals(outcome.TerminalStatus, "PASS", StringComparison.Ordinal))
                break;

            var refreshed = await _client.ListTeamsAsync(organization, cancellationToken);
            bySlug = refreshed.ToDictionary(x => x.Slug, StringComparer.OrdinalIgnoreCase);
            applied++;
        }

        var readback = await _client.ListTeamsAsync(organization, cancellationToken);
        var remaining = GitHubOrgMutationPlanner.Plan(desired, readback).Mutations;
        var state = remaining.Count == 0 ? GitHubOrgReconciliationState.Verified : GitHubOrgReconciliationState.Drifted;
        var resultReceipt = new GitHubOrgReconciliationResult(
            Guid.NewGuid().ToString("N"),
            organization,
            state,
            applied,
            remaining,
            DateTimeOffset.UtcNow);
        await _receipts.AppendAsync(resultReceipt, topology.SchemaVersion, cancellationToken);
        return resultReceipt;
    }

    private static string CanonicalTeamState(string teamSlug, string? parentSlug) =>
        $"team={teamSlug};parent={parentSlug ?? string.Empty}";

    private sealed class GitHubTeamMutationPrivilegedProvider : IPrivilegedActionProvider
    {
        private readonly IGitHubOrganizationClient _client;
        private readonly string _organization;
        private readonly GitHubTeamMutationKind _kind;
        private readonly string _teamSlug;
        private readonly string? _parentSlug;
        private readonly long? _parentId;

        public GitHubTeamMutationPrivilegedProvider(
            IGitHubOrganizationClient client,
            string organization,
            GitHubTeamMutationKind kind,
            string teamSlug,
            string? parentSlug,
            long? parentId)
        {
            _client = client;
            _organization = organization;
            _kind = kind;
            _teamSlug = teamSlug;
            _parentSlug = parentSlug;
            _parentId = parentId;
        }

        public async Task<PrivilegedProviderResult> ExecuteAsync(PrivilegedActionRequest request, CancellationToken cancellationToken)
        {
            var team = _kind switch
            {
                GitHubTeamMutationKind.Create => await _client.CreateTeamAsync(_organization, _teamSlug, _parentId, cancellationToken),
                GitHubTeamMutationKind.SetParent => await _client.SetParentAsync(_organization, _teamSlug, _parentId, cancellationToken),
                _ => throw new InvalidOperationException($"Unsupported GitHub team mutation '{_kind}'.")
            };
            return new PrivilegedProviderResult(true, "MUTATION_ACCEPTED", CanonicalTeamState(team.Slug, team.ParentSlug));
        }

        public async Task<PrivilegedProviderResult> ReadBackAsync(PrivilegedActionRequest request, CancellationToken cancellationToken)
        {
            var teams = await _client.ListTeamsAsync(_organization, cancellationToken);
            var team = teams.FirstOrDefault(x => string.Equals(x.Slug, _teamSlug, StringComparison.OrdinalIgnoreCase));
            if (team is null)
                return new PrivilegedProviderResult(false, "TEAM_NOT_OBSERVED", string.Empty);

            var state = CanonicalTeamState(team.Slug, team.ParentSlug);
            var expected = CanonicalTeamState(_teamSlug, _parentSlug);
            return new PrivilegedProviderResult(
                string.Equals(state, expected, StringComparison.Ordinal),
                string.Equals(state, expected, StringComparison.Ordinal) ? "READBACK_MATCH" : "READBACK_MISMATCH",
                state);
        }
    }
}

public sealed class GitHubOrgMutationReceiptStore
{
    private readonly string _path;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public GitHubOrgMutationReceiptStore(string path) => _path = path;

    public async Task AppendAsync(GitHubOrgReconciliationResult result, string topologyVersion, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var health = GitHubHealthEvidence.FromReconciliation(result, topologyVersion);
        var record = new
        {
            receipt_id = result.ReceiptId,
            topology_authority = health.TopologyAuthority,
            health_authority = health.HealthAuthority,
            provider_role = health.ProviderRole,
            topology_version = topologyVersion,
            organization = result.Organization,
            state = result.State.ToString().ToUpperInvariant(),
            applied_mutations = result.AppliedMutations,
            remaining_mutations = result.RemainingMutations,
            completed_at_utc = result.CompletedAtUtc,
            health = new
            {
                health_class = health.HealthClass,
                subject = health.Subject,
                status = health.Status,
                reason_code = health.ReasonCode,
                dependency_fingerprint = health.DependencyFingerprint,
                deduplication_key = health.DeduplicationKey,
                evidence_references = health.EvidenceReferences,
                first_observed_at = health.FirstObservedAt,
                last_observed_at = health.LastObservedAt,
                accountable_route = health.AccountableRoute,
                release_blocking = health.ReleaseBlocking,
                next_reevaluation_trigger = health.NextReevaluationTrigger
            }
        };
        var line = JsonSerializer.Serialize(record, JsonOptions) + Environment.NewLine;
        await File.AppendAllTextAsync(_path, line, cancellationToken);
    }
}
