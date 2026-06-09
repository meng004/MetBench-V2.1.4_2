
namespace MetBench_Client.Views.Pages
{
    public partial class MetaPatternsPage
    {
        public ViewModels.MetaPatternsViewModel ViewModel { get; }

        public MetaPatternsPage(ViewModels.MetaPatternsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
