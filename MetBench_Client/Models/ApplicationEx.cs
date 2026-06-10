using CommunityToolkit.Mvvm.ComponentModel;
using MetBench_Domain;

namespace MetBench_Client.Models
{
    public partial class ApplicationEx : ObservableObject
    {
        public Application Application { get; private set; }

        [ObservableProperty]
        private bool _isChecked;

        public ApplicationEx(Application application)
        {
            Application = application;
        }

        // 让 WPF ComboBox 默认按 wrapped Application.Name 显示, 而不是 fallback 到 "MetBench_Client.Models.ApplicationEx" 类名 (UAT round-1 UC-A5 bug)
        public override string ToString() => Application?.Name ?? string.Empty;
    }
}
