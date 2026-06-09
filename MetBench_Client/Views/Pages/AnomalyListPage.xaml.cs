
namespace MetBench_Client.Views.Pages
{
    /// <summary>
    /// Interaction logic for AnomalyListPage.xaml
    /// </summary>
    public partial class AnomalyListPage
    {
        public ViewModels.AnomalyListViewModel ViewModel { get; }

        public AnomalyListPage(ViewModels.AnomalyListViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
