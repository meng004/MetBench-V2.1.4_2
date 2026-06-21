using MetBench_Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();

var app = builder.Build();
app.MapSystemMtApi();
app.Run();

public partial class Program;
