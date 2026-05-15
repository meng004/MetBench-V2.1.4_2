using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Pages
{
    public partial class TrendDashboardPage : INavigableView<ViewModels.TrendDashboardViewModel>
    {
        public ViewModels.TrendDashboardViewModel ViewModel { get; }

        public TrendDashboardPage(ViewModels.TrendDashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
