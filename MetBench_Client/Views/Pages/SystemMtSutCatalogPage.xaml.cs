using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Pages
{
    public partial class SystemMtSutCatalogPage : INavigableView<ViewModels.SystemMtSutCatalogViewModel>
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
