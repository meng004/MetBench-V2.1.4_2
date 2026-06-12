using MetBench_Client.ViewModels;
using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Pages
{
    public partial class SystemMtRuntimeEnvironmentPage : INavigableView<SystemMtRuntimeEnvironmentViewModel>
    {
        public SystemMtRuntimeEnvironmentPage(SystemMtRuntimeEnvironmentViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }

        public SystemMtRuntimeEnvironmentViewModel ViewModel { get; }
    }
}
