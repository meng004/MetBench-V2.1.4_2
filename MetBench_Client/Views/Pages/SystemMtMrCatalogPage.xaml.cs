using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Pages
{
    public partial class SystemMtMrCatalogPage : INavigableView<ViewModels.SystemMtMrCatalogViewModel>
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
