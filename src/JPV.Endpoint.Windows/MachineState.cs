using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace JPV.Endpoint.Windows;

public sealed class MachineState
{
    public string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "JPV", "Endpoint");
    public string IdentityPath => Path.Combine(Root, "machine.json");
    public string SecretPath => Path.Combine(Root, "machine.secret.dpapi");
    public string ReceiptPath => Path.Combine(Root, "endpoint-receipt.json");
    public string PolicyPath => Path.Combine(Root, "policy.json");

    public MachineIdentity LoadOrCreateIdentity()
    {
        Directory.CreateDirectory(Root);
        if (File.Exists(IdentityPath) && File.Exists(SecretPath))
        {
            var metadata = JsonSerializer.Deserialize<MachineIdentityMetadata>(File.ReadAllText(IdentityPath))
                ?? throw new InvalidOperationException("JPV machine identity metadata is invalid.");
            var secret = ProtectedData.Unprotect(File.ReadAllBytes(SecretPath), null, DataProtectionScope.LocalMachine);
            return new MachineIdentity(metadata.MachineName, Convert.ToHexString(secret));
        }

        var rawSecret = RandomNumberGenerator.GetBytes(32);
        var protectedSecret = ProtectedData.Protect(rawSecret, null, DataProtectionScope.LocalMachine);
        WriteAtomicBytes(SecretPath, protectedSecret);
        WriteAtomic(IdentityPath, JsonSerializer.Serialize(new MachineIdentityMetadata(Environment.MachineName)));
        return new MachineIdentity(Environment.MachineName, Convert.ToHexString(rawSecret));
    }

    public EndpointPolicy LoadPolicy()
    {
        if (!File.Exists(PolicyPath))
            throw new InvalidOperationException("JPV endpoint policy is absent; execution fails closed.");
        var policy = JsonSerializer.Deserialize<EndpointPolicy>(File.ReadAllText(PolicyPath));
        if (policy is null || !policy.Enabled || policy.AllowedTransports is null || policy.AllowedTransports.Length == 0)
            throw new InvalidOperationException("JPV endpoint policy is disabled, invalid, or admits no transport.");
        return policy;
    }

    public static void WriteAtomic(string path, string content) =>
        WriteAtomicBytes(path, Encoding.UTF8.GetBytes(content));

    private static void WriteAtomicBytes(string path, byte[] content)
    {
        var tmp = path + ".tmp";
        File.WriteAllBytes(tmp, content);
        File.Move(tmp, path, true);
    }
}

public sealed record MachineIdentityMetadata(string MachineName);
public sealed record MachineIdentity(string MachineName, string MachineSecret);
public sealed record EndpointPolicy(bool Enabled, string[] AllowedTransports);
