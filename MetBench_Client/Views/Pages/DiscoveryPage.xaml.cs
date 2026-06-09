
namespace MetBench_Client.Views.Pages
{
    public partial class DiscoveryPage
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
