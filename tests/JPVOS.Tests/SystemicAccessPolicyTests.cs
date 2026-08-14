using JPVOS.Services.SystemicAccess;

namespace JPVOS.Tests;

public sealed class SystemicAccessPolicyTests
{
    [Fact]
    public void LoadAndValidate_RejectsMissingFile()
    {
        var path = Path.Join(Path.GetTempPath(), Guid.NewGuid() + ".json");
        Assert.Throws<InvalidOperationException>(() => SystemicAccessPolicyLoader.LoadAndValidate(path));
    }

    [Fact]
    public void LoadAndValidate_RejectsWeakenedPolicy()
    {
        var path = WritePolicy("""
        {
          "id": "JPV-GOV-SYSTEMIC-ACCESS-HYGIENE-001",
          "canonical_source": {
            "repository": "jaypVLabs/JPV-OS",
            "path": "governance/policies/systemic-access-hygiene.v1.json",
            "baseline_commit": "6212c25165285812051ff601d6385facb898bf71"
          },
          "actions": ["VALID","QUARANTINE","REVOKE","ROTATE","DEDUPLICATE","EXPIRE","REVIEW"],
          "interpretation": { "systemic_only": true, "person_targeting": true },
          "default_uncertain_action": "QUARANTINE",
          "preserve_audit_receipt": true,
          "re_enable_requires": "explicit_current_authorization"
        }
        """);

        try
        {
            Assert.Throws<InvalidOperationException>(() => SystemicAccessPolicyLoader.LoadAndValidate(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void LoadAndValidate_RejectsMissingCanonicalSource()
    {
        var path = WritePolicy("""
        {
          "id": "JPV-GOV-SYSTEMIC-ACCESS-HYGIENE-001",
          "actions": ["VALID","QUARANTINE","REVOKE","ROTATE","DEDUPLICATE","EXPIRE","REVIEW"],
          "interpretation": { "systemic_only": true, "person_targeting": false },
          "default_uncertain_action": "QUARANTINE",
          "preserve_audit_receipt": true,
          "re_enable_requires": "explicit_current_authorization"
        }
        """);

        try
        {
            Assert.Throws<InvalidOperationException>(() => SystemicAccessPolicyLoader.LoadAndValidate(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void LoadAndValidate_AcceptsCanonicalPolicy()
    {
        var path = WritePolicy("""
        {
          "id": "JPV-GOV-SYSTEMIC-ACCESS-HYGIENE-001",
          "canonical_source": {
            "repository": "jaypVLabs/JPV-OS",
            "path": "governance/policies/systemic-access-hygiene.v1.json",
            "baseline_commit": "6212c25165285812051ff601d6385facb898bf71"
          },
          "actions": ["VALID","QUARANTINE","REVOKE","ROTATE","DEDUPLICATE","EXPIRE","REVIEW"],
          "interpretation": { "systemic_only": true, "person_targeting": false },
          "default_uncertain_action": "QUARANTINE",
          "preserve_audit_receipt": true,
          "re_enable_requires": "explicit_current_authorization"
        }
        """);

        try
        {
            var policy = SystemicAccessPolicyLoader.LoadAndValidate(path);
            Assert.Equal("JPV-GOV-SYSTEMIC-ACCESS-HYGIENE-001", policy.Id);
            Assert.Equal("jaypVLabs/JPV-OS", policy.CanonicalSource.Repository);
        }
        finally { File.Delete(path); }
    }

    private static string WritePolicy(string content)
    {
        var path = Path.Join(Path.GetTempPath(), Guid.NewGuid() + ".json");
        File.WriteAllText(path, content);
        return path;
    }
}
