using MetBench_Api;
using MetBench_BLL.SystemMT.ControlPlane;
using MetBench_BLL.SystemMT.Jobs;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IJobQueue, ChannelJobQueue>();
builder.Services.AddSingleton<IJobStore, InMemoryJobStore>();
builder.Services.AddSingleton<IJobCancellationRegistry, JobCancellationRegistry>();
builder.Services.AddSingleton<ISystemMtJobService, SystemMtJobService>();
builder.Services.AddSingleton<ISystemMtControlPlaneService, SystemMtControlPlaneService>();

var app = builder.Build();
app.MapSystemMtApi();
app.Run();

public partial class Program;
