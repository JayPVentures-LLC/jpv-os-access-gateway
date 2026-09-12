using System.Security.Cryptography;
using System.Text;
using JPVOS.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace JPVOS.Tests;

public sealed class MachineReadAuthTests
{
    [Fact]
    public void AcceptsOnlyMatchingBearerToken()
    {
        const string token = "test-machine-read-token";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [MachineReadAuth.TokenHashConfigurationKey] = hash
        }).Build();
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {token}";
        Assert.True(MachineReadAuth.IsAuthorized(context.Request, configuration));

        context.Request.Headers.Authorization = "Bearer wrong-token";
        Assert.False(MachineReadAuth.IsAuthorized(context.Request, configuration));
    }

    [Fact]
    public void FailsClosedWhenTokenHashIsNotProvisioned()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer anything";
        Assert.False(MachineReadAuth.IsAuthorized(context.Request, new ConfigurationBuilder().Build()));
    }
}
