
namespace MetBench_Client.Views.Pages
{
    public partial class SystemMtSutCatalogPage
    {
        public ViewModels.SystemMtSutCatalogViewModel ViewModel { get; }

        public SystemMtSutCatalogPage(ViewModels.SystemMtSutCatalogViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
