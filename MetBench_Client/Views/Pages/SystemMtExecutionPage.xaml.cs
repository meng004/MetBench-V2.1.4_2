
namespace MetBench_Client.Views.Pages
{
    public partial class SystemMtExecutionPage
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
