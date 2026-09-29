using JPV.Endpoint.Windows;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "JPV.NativeEndpoint");
builder.Services.AddSingleton<MachineState>();
builder.Services.AddHostedService<EndpointWorker>();
await builder.Build().RunAsync();
