
namespace MetBench_Client.Views.Pages
{
    /// <summary>
    /// Interaction logic for ReplayResultPage.xaml
    /// </summary>
    public partial class ReplayResultPage
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
