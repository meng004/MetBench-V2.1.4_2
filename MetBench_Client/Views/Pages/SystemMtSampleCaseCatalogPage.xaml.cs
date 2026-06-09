
namespace MetBench_Client.Views.Pages
{
    public partial class SystemMtSampleCaseCatalogPage
    {
        public ViewModels.SystemMtSampleCaseCatalogViewModel ViewModel { get; }

        public SystemMtSampleCaseCatalogPage(ViewModels.SystemMtSampleCaseCatalogViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
