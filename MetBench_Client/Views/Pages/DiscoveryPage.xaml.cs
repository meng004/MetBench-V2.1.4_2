using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Pages
{
    public partial class DiscoveryPage : INavigableView<ViewModels.DiscoveryViewModel>
    {
        public ViewModels.DiscoveryViewModel ViewModel { get; }

        public DiscoveryPage(ViewModels.DiscoveryViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
