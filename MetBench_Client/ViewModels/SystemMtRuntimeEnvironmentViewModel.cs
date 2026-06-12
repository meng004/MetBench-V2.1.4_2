using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.SystemMT.Runtime;
using MetBench_UI.Localization;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels
{
    public sealed partial class SystemMtRuntimeEnvironmentViewModel : ObservableObject, INavigationAware
    {
        private readonly IDockerMcpRuntimeProfileStore _store;
        private readonly IDockerMcpRuntimeClient _client;

        public SystemMtRuntimeEnvironmentViewModel(
            IDockerMcpRuntimeProfileStore store,
            IDockerMcpRuntimeClient client,
            LocalizedTextProvider localization)
        {
            _store = store;
            _client = client;
            Localization = localization;
            StatusMessage = Localization["RuntimeEnv_Status_Ready"];
        }

        public LocalizedTextProvider Localization { get; }

        [ObservableProperty]
        private ObservableCollection<RuntimeEnvironmentRow> _runtimeProfiles = new();

        [ObservableProperty]
        private string _runtimeKey = "docker-linux";

        [ObservableProperty]
        private string _endpoint = "http://192.168.1.42:8765";

        [ObservableProperty]
        private string _image = "metbench/runtime-python:latest";

        [ObservableProperty]
        private string _pythonExecutable = "python3";

        [ObservableProperty]
        private string _authTokenEnvironmentVariable = "METBENCH_DOCKER_MCP_TOKEN";

        [ObservableProperty]
        private string _statusMessage;

        public void OnNavigatedTo()
        {
            RefreshRuntimeProfiles();
        }

        public void OnNavigatedFrom()
        {
        }

        [RelayCommand]
        private void RefreshRuntimeProfiles()
        {
            var rows = new ObservableCollection<RuntimeEnvironmentRow>();
            foreach (var pair in _store.LoadRuntimePythons())
            {
                var backend = pair.Value.StartsWith("docker-mcp://", StringComparison.OrdinalIgnoreCase)
                    ? "Docker MCP"
                    : "Local Python";
                rows.Add(new RuntimeEnvironmentRow(pair.Key, backend, pair.Value));
            }

            RuntimeProfiles = rows;
            StatusMessage = string.Format(Localization["RuntimeEnv_Status_Loaded_Fmt"], RuntimeProfiles.Count);
        }

        [RelayCommand]
        private void SaveProfile()
        {
            try
            {
                _store.Save(CreateDraft());
                RefreshRuntimeProfiles();
                StatusMessage = string.Format(Localization["RuntimeEnv_Status_Saved_Fmt"], RuntimeKey.Trim());
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(Localization["RuntimeEnv_Status_SaveFailed_Fmt"], ex.Message);
            }
        }

        [RelayCommand]
        private async Task TestConnectionAsync()
        {
            try
            {
                var draft = CreateDraft();
                var health = await _client.HealthAsync(new DockerMcpRuntimeOptions(
                    draft.Endpoint.Trim(),
                    draft.Image.Trim(),
                    draft.PythonExecutable.Trim(),
                    string.IsNullOrWhiteSpace(draft.AuthTokenEnvironmentVariable)
                        ? null
                        : draft.AuthTokenEnvironmentVariable.Trim()));
                StatusMessage = health.Available
                    ? string.Format(Localization["RuntimeEnv_Status_HealthOk_Fmt"], health.BindHost, health.BindPort)
                    : string.Format(Localization["RuntimeEnv_Status_HealthFailed_Fmt"], health.Status, health.Detail);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(Localization["RuntimeEnv_Status_HealthFailed_Fmt"], "error", ex.Message);
            }
        }

        private DockerMcpRuntimeProfileDraft CreateDraft() => new(
            RuntimeKey,
            Endpoint,
            Image,
            PythonExecutable,
            string.IsNullOrWhiteSpace(AuthTokenEnvironmentVariable)
                ? null
                : AuthTokenEnvironmentVariable);
    }

    public sealed record RuntimeEnvironmentRow(string RuntimeKey, string Backend, string Value);
}
