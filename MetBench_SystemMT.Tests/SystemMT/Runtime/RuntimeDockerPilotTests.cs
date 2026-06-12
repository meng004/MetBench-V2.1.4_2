using MetBench_BLL.SystemMT.Pipeline;
using MetBench_BLL.SystemMT.Runtime;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Runtime;

public sealed class RuntimeDockerPilotTests
{
    [SkippableFact]
    public async Task Configured_docker_runtime_image_runs_real_container_probe()
    {
        var image = Environment.GetEnvironmentVariable("METBENCH_DOCKER_RUNTIME_IMAGE");
        Skip.If(
            string.IsNullOrWhiteSpace(image),
            "Set METBENCH_DOCKER_RUNTIME_IMAGE to an already-built local image, e.g. metbench-runtime:latest, to run the real Docker runtime pilot.");

        var profile = RuntimeProfile.DockerContainer(
            "docker-sciml",
            "SciML Docker runtime",
            image!,
            "python --version",
            TimeSpan.FromSeconds(90));
        var service = new RuntimePreflightService(new DefaultProcessExecutor());

        var result = await service.CheckAsync(profile);

        Assert.True(result.Passed, result.Detail);
        var containerProbe = Assert.Single(result.Diagnostics, d => d.Name == "Docker container probe");
        Assert.True(containerProbe.Passed, containerProbe.Detail);
        Assert.Contains("Python", containerProbe.Stdout + containerProbe.Stderr, StringComparison.OrdinalIgnoreCase);
    }
}
