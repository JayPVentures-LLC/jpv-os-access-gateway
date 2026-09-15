namespace JPVOS.Services;

public static class PeopleProtectionStartupGuard
{
    public static void Verify(WebApplication app)
    {
        var requiredFile = Path.Combine(app.Environment.ContentRootPath, "PEOPLE-PROTECTION-NON-NEGOTIABLE.md");
        if (!File.Exists(requiredFile)) throw new InvalidOperationException("BOOT_BLOCKED: Required People Protection policy artifact is missing.");
        var content = File.ReadAllText(requiredFile);
        var requiredTerms = new[] { "human dignity","autonomy","consent","anti-discrimination","forced labor","slavery","human review","appeal","reversible","auditable","social scoring","unlawful surveillance","biometric","children","monetization","data resale","interoperability","vendor exclusivity","lock-in" };
        var missing = requiredTerms.Where(term => !content.Contains(term, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (missing.Length > 0) throw new InvalidOperationException("BOOT_BLOCKED: People Protection policy artifact is incomplete. Missing: " + string.Join(", ", missing));

        var agencySafetyPath = Path.Combine(app.Environment.ContentRootPath, ".jpv", "governance", "ai-agency-safety.json");
        _ = JPVOS.Services.AgencySafety.AgencySafetyPolicyLoader.LoadAndValidate(agencySafetyPath);
    }
}
