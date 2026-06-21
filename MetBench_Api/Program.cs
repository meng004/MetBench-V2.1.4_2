using MetBench_Api;
using MetBench_BLL.SystemMT.Anomaly;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.ControlPlane;
using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_DAL;
using MetBench_IDAL;
using LiteDB;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddSystemMtRepositories();
builder.Services.AddSingleton<IJobQueue, ChannelJobQueue>();
builder.Services.AddSingleton<IJobStore, InMemoryJobStore>();
builder.Services.AddSingleton<IJobCancellationRegistry, JobCancellationRegistry>();
builder.Services.AddSingleton<ISystemMtJobService, SystemMtJobService>();
builder.Services.AddSingleton<ISystemMtControlPlaneService, SystemMtControlPlaneService>();
builder.Services.AddSingleton(provider =>
{
    var dataDir = builder.Configuration["MetBench:DataDir"] ?? AppContext.BaseDirectory;
    Directory.CreateDirectory(dataDir);
    return new LiteDatabase(
        $"Filename={Path.Combine(dataDir, "SystemMT.Litedb")};UTC_DATE=true",
        new BsonMapper());
});
builder.Services.AddSingleton<ISystemMtResultRepository>(provider =>
    new LiteDbSystemMtResultRepository(provider.GetRequiredService<LiteDatabase>()));
builder.Services.AddSingleton<IExecutionEvidenceRepository>(provider =>
    new LiteDbExecutionEvidenceRepository(provider.GetRequiredService<LiteDatabase>()));
builder.Services.AddSingleton(provider =>
{
    var runtimePythons = builder.Configuration
        .GetSection("LauncherOptions:RuntimePythons")
        .Get<Dictionary<string, string>>();
    var sutRoot = builder.Configuration["LauncherOptions:SutRoot"]
        ?? Path.Combine(AppContext.BaseDirectory, "SUT");
    var systemPython = Environment.GetEnvironmentVariable("METBENCH_SYSTEM_PYTHON")
        ?? builder.Configuration["LauncherOptions:SystemPython"]
        ?? "python3";
    var openMocPython = Environment.GetEnvironmentVariable("METBENCH_OPENMOC_PYTHON")
        ?? builder.Configuration["LauncherOptions:OpenMocPython"]
        ?? systemPython;
    return new LauncherOptions(sutRoot, systemPython, openMocPython, RuntimePythons: runtimePythons);
});
builder.Services.AddScoped<IMrCatalogProvider, ManifestMrCatalogProvider>();
builder.Services.AddScoped<ISystemMtPipeline, SystemMtPipeline>();
builder.Services.AddScoped<SystemMtExecutionRecorder>();
builder.Services.AddScoped<IAnomalyService, AnomalyService>();
builder.Services.AddScoped<ISystemMtLauncher>(provider => new SystemMtLauncher(
    provider.GetRequiredService<LauncherOptions>(),
    provider.GetRequiredService<ISystemMtPipeline>(),
    provider.GetRequiredService<SystemMtExecutionRecorder>(),
    provider.GetRequiredService<IAnomalyService>(),
    provider.GetRequiredService<IMrCatalogProvider>(),
    AnomalySeverityThresholds.Default));
builder.Services.AddScoped<ISystemMtAsyncPipeline, SystemMtAsyncPipeline>();
builder.Services.AddHostedService<SystemMtApiJobWorkerHostedService>();

var app = builder.Build();
app.MapSystemMtApi();
app.Run();

public partial class Program;
