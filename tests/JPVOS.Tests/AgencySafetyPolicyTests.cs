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
        bool securityTest=false,
        string? securityAuthId=null,
        string? securityTarget=null,
        string? method=null,
        DateTimeOffset? validUntil=null) =>
        new("human:founder","auth-1","EXECUTE_BOUNDED","repo:bounded",["read"],300,"example.com","none",null,"none",false,false,null,false,
            target,securityTest,securityAuthId,securityTarget,method,validUntil);

    [Fact]
    public void Prior_denial_is_sticky_across_new_authorizer_instances()
    {
        var dir = Path.Combine(Path.GetTempPath(), "jpv-agency-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "denials.json");
            var first = new AgencySafetyAuthorizer(Policy(), new FileAgencyDenialStateStore(path));
            first.RecordAuthoritativeTargetDenial("target:example", "deny-1");

            var retry = new AgencySafetyAuthorizer(Policy(), new FileAgencyDenialStateStore(path));
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
            var authorizer = new AgencySafetyAuthorizer(Policy(), new FileAgencyDenialStateStore(path));
            authorizer.RecordAuthoritativeTargetDenial("target:example", "deny-1");

            Assert.False(authorizer.Authorize(Request(securityTest:true,securityAuthId:"auth-1",securityTarget:"target:example",method:"GET",validUntil:DateTimeOffset.UtcNow.AddHours(1))).Allowed);
            Assert.False(authorizer.Authorize(Request(securityTest:true,securityAuthId:"sec-2",securityTarget:"other",method:"GET",validUntil:DateTimeOffset.UtcNow.AddHours(1))).Allowed);
            Assert.False(authorizer.Authorize(Request(securityTest:true,securityAuthId:"sec-2",securityTarget:"target:example",method:"GET",validUntil:DateTimeOffset.UtcNow.AddMinutes(-1))).Allowed);
            Assert.True(authorizer.Authorize(Request(securityTest:true,securityAuthId:"sec-2",securityTarget:"target:example",method:"GET",validUntil:DateTimeOffset.UtcNow.AddHours(1))).Allowed);
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
            var authorizer = new AgencySafetyAuthorizer(Policy(), new FileAgencyDenialStateStore(path));
            authorizer.RecordAuthoritativeTargetDenial("target:blocked", "deny-1");

            var result = authorizer.Authorize(Request(target:"target:public"));

            Assert.True(result.Allowed);
        }
        finally { Directory.Delete(dir, true); }
    }
}
