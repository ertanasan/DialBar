using PcMeter.Core;
using PcMeter.Helper;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = HelperPipe.ServiceName);
builder.Services.AddHostedService<SensorWorker>();
builder.Build().Run();
