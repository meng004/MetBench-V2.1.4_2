namespace MetBench_BLL.SystemMT.Runtime;

public interface IRuntimeProfileProvider
{
    RuntimeProfile GetProfile(string? runtimeKey);
}
