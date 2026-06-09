
namespace MetBench_Client.Views.Pages
{
    public partial class SystemMtEquationCatalogPage
    {
        public ViewModels.SystemMtEquationCatalogViewModel ViewModel { get; }

        public SystemMtEquationCatalogPage(ViewModels.SystemMtEquationCatalogViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
