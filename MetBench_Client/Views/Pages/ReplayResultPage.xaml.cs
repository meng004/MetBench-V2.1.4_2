using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Pages
{
    /// <summary>
    /// Interaction logic for ReplayResultPage.xaml
    /// </summary>
    public partial class ReplayResultPage : INavigableView<ViewModels.ReplayResultViewModel>
    {
        public ViewModels.ReplayResultViewModel ViewModel { get; }

        public ReplayResultPage(ViewModels.ReplayResultViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
