using System.Text.Json;

namespace JPV.Endpoint.Windows;

public sealed class EndpointWorker(MachineState state, ILogger<EndpointWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var identity = state.LoadOrCreateIdentity();
        log.LogInformation("JPV native endpoint online for {Machine}", identity.MachineName);

        while (!stoppingToken.IsCancellationRequested)
        {
            string status;
            string[] transports = [];
            try
            {
                var policy = state.LoadPolicy();
                transports = policy.AllowedTransports;
                status = "ready";
            }
            catch (Exception ex)
            {
                status = "fail_closed";
                log.LogWarning("{Message}", ex.Message);
            }

            var receipt = new
            {
                schemaVersion = "1.0",
                endpoint = "JPV.NativeEndpoint",
                machine = identity.MachineName,
                processId = Environment.ProcessId,
                sessionId = Environment.ProcessId == 0 ? -1 : System.Diagnostics.Process.GetCurrentProcess().SessionId,
                status,
                machineScoped = true,
                interactiveSessionRequired = false,
                adaptersAreAuthority = false,
                allowedTransports = transports,
                observedAtUtc = DateTimeOffset.UtcNow
            };
            MachineState.WriteAtomic(state.ReceiptPath, JsonSerializer.Serialize(receipt));
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}
