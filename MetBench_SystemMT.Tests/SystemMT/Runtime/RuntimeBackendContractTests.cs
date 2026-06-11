using MetBench_BLL.SystemMT.Runtime;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Runtime;

public sealed class RuntimeBackendContractTests
{
    [Fact]
    public void Docker_contract_projects_to_non_executable_runtime_placeholder()
    {
        var contract = RuntimeBackendContract.Docker(
            "sciml-mgn-docker",
            "metbench/sciml-mgn:cpu",
            runtimeKey: "docker-sciml-mgn");

        var profile = contract.ToRuntimeProfile();

        Assert.Equal(RuntimeBackendKind.Docker, contract.Kind);
        Assert.Equal(RuntimeKind.DockerPlaceholder, profile.Kind);
        Assert.False(profile.IsExecutableInV1);
        Assert.Equal("metbench/sciml-mgn:cpu", contract.Settings["image"]);
    }

    [Fact]
    public void Ssh_contract_projects_to_non_executable_runtime_placeholder()
    {
        var contract = RuntimeBackendContract.SshRemote(
            "sciml-mgn-ssh",
            "gpu01.example.org",
            "/data/mgn/cylinder-flow",
            runtimeKey: "ssh-sciml-mgn");

        var profile = contract.ToRuntimeProfile();

        Assert.Equal(RuntimeBackendKind.SshRemote, contract.Kind);
        Assert.Equal(RuntimeKind.RemotePlaceholder, profile.Kind);
        Assert.False(profile.IsExecutableInV1);
        Assert.Equal("gpu01.example.org", contract.Settings["host"]);
        Assert.Equal("/data/mgn/cylinder-flow", contract.Settings["remote_root"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Docker_contract_rejects_blank_image(string image)
    {
        Assert.Throws<ArgumentException>(() =>
            RuntimeBackendContract.Docker("docker", image));
    }

    [Theory]
    [InlineData("", "/data")]
    [InlineData("gpu01", "")]
    public void Ssh_contract_rejects_blank_required_fields(string host, string remoteRoot)
    {
        Assert.Throws<ArgumentException>(() =>
            RuntimeBackendContract.SshRemote("ssh", host, remoteRoot));
    }
}
