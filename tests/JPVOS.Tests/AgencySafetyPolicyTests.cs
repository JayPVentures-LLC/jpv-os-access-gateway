using JPVOS.Services.AgencySafety;
using Xunit;

namespace JPVOS.Tests;

public sealed class AgencySafetyPolicyTests
{
    private static AgencySafetyPolicy Policy() => new(
        "JPV-GOV-AI-AGENCY-SAFETY-001","fail-closed",true,false,false,false,false,true,true,true,true,true,true,
        ["OBSERVE","ANALYZE","RECOMMEND","PREPARE_REVERSIBLE","EXECUTE_BOUNDED","EXECUTE_CONSEQUENTIAL","EXECUTE_IRREVERSIBLE_HIGH_IMPACT"]);

    private static AgencyActionRequest Request(
        string target="target:example",
        string method="GET",
        string? securityAuthId=null) =>
        new("human:founder","auth-1","EXECUTE_BOUNDED","repo:bounded",["read"],300,"example.com","none",null,"none",false,false,null,false,
            target,method,securityAuthId);

    [Fact]
    public void Prior_denial_is_sticky_across_new_authorizer_instances()
    {
        var dir = Path.Combine(Path.GetTempPath(), "jpv-agency-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "denials.json");
            var grants = Path.Combine(dir, "grants.json");
            var first = new AgencySafetyAuthorizer(Policy(), new FileAgencyDenialStateStore(path), new FileAgencySecurityTestingGrantStore(grants));
            first.RecordAuthoritativeTargetDenial("target:example", "deny-1");

            var retry = new AgencySafetyAuthorizer(Policy(), new FileAgencyDenialStateStore(path), new FileAgencySecurityTestingGrantStore(grants));
            var result = retry.Authorize(Request());

            Assert.False(result.Allowed);
            Assert.Equal("third_party_authorization_denial_circumvention", result.Reason);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void Security_test_exception_requires_distinct_scoped_unexpired_authorization()
    {
        var dir = Path.Combine(Path.GetTempPath(), "jpv-agency-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "denials.json");
            var grants = Path.Combine(dir, "grants.json");
            var authorizer = new AgencySafetyAuthorizer(Policy(), new FileAgencyDenialStateStore(path), new FileAgencySecurityTestingGrantStore(grants));
            authorizer.RecordAuthoritativeTargetDenial("target:example", "deny-1");

            Assert.False(authorizer.Authorize(Request(securityAuthId:"sec-2")).Allowed);
            authorizer.RecordSecurityTestingGrant(new("sec-2","other","GET",DateTimeOffset.UtcNow.AddHours(1),"human:founder"));
            Assert.False(authorizer.Authorize(Request(securityAuthId:"sec-2")).Allowed);
            authorizer.RecordSecurityTestingGrant(new("sec-2","target:example","GET",DateTimeOffset.UtcNow.AddMinutes(-1),"human:founder"));
            Assert.False(authorizer.Authorize(Request(securityAuthId:"sec-2")).Allowed);
            authorizer.RecordSecurityTestingGrant(new("sec-2","target:example","GET",DateTimeOffset.UtcNow.AddHours(1),"human:founder"));
            Assert.True(authorizer.Authorize(Request(securityAuthId:"sec-2")).Allowed);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void Denial_on_one_target_does_not_block_unrelated_public_target()
    {
        var dir = Path.Combine(Path.GetTempPath(), "jpv-agency-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "denials.json");
            var authorizer = new AgencySafetyAuthorizer(Policy(), new FileAgencyDenialStateStore(path), new FileAgencySecurityTestingGrantStore(Path.Combine(dir, "grants.json")));
            authorizer.RecordAuthoritativeTargetDenial("target:blocked", "deny-1");

            var result = authorizer.Authorize(Request(target:"target:public"));

            Assert.True(result.Allowed);
        }
        finally { Directory.Delete(dir, true); }
    }
}
