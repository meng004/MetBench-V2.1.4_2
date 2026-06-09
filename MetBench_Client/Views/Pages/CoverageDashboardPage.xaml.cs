
namespace MetBench_Client.Views.Pages
{
    public partial class CoverageDashboardPage
    {
        public ViewModels.CoverageDashboardViewModel ViewModel { get; }

        public CoverageDashboardPage(ViewModels.CoverageDashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
