using DialBar.Core;
using DialBar.Helper;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = HelperPipe.ServiceName);
builder.Services.AddHostedService<SensorWorker>();
builder.Build().Run();
