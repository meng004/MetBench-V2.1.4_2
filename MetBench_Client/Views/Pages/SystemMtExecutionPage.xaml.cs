using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Pages
{
    public partial class SystemMtExecutionPage : INavigableView<ViewModels.SystemMtExecutionViewModel>
    {
        public ViewModels.SystemMtExecutionViewModel ViewModel { get; }

        public SystemMtExecutionPage(ViewModels.SystemMtExecutionViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
