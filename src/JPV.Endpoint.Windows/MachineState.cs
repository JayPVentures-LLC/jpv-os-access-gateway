using System.Security.Cryptography;
using System.Text.Json;

namespace JPV.Endpoint.Windows;

public sealed class MachineState
{
    public string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "JPV", "Endpoint");
    public string IdentityPath => Path.Combine(Root, "machine.json");
    public string ReceiptPath => Path.Combine(Root, "endpoint-receipt.json");
    public string PolicyPath => Path.Combine(Root, "policy.json");

    public MachineIdentity LoadOrCreateIdentity()
    {
        Directory.CreateDirectory(Root);
        if (File.Exists(IdentityPath))
            return JsonSerializer.Deserialize<MachineIdentity>(File.ReadAllText(IdentityPath))
                ?? throw new InvalidOperationException("JPV machine identity is invalid.");

        var id = new MachineIdentity(Environment.MachineName, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
        WriteAtomic(IdentityPath, JsonSerializer.Serialize(id));
        return id;
    }

    public EndpointPolicy LoadPolicy()
    {
        if (!File.Exists(PolicyPath))
            throw new InvalidOperationException("JPV endpoint policy is absent; execution fails closed.");
        var policy = JsonSerializer.Deserialize<EndpointPolicy>(File.ReadAllText(PolicyPath));
        if (policy is null || !policy.Enabled) throw new InvalidOperationException("JPV endpoint policy is disabled or invalid.");
        return policy;
    }

    public static void WriteAtomic(string path, string content)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, content);
        File.Move(tmp, path, true);
    }
}

public sealed record MachineIdentity(string MachineName, string MachineSecret);
public sealed record EndpointPolicy(bool Enabled, string[] AllowedTransports);
