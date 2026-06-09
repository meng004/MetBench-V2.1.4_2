
namespace MetBench_Client.Views.Pages
{
    public partial class SystemMtMrCatalogPage
    {
        public ViewModels.SystemMtMrCatalogViewModel ViewModel { get; }

        public SystemMtMrCatalogPage(ViewModels.SystemMtMrCatalogViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
