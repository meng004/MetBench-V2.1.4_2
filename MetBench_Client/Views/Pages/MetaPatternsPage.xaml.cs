using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Pages
{
    public partial class MetaPatternsPage : INavigableView<ViewModels.MetaPatternsViewModel>
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
