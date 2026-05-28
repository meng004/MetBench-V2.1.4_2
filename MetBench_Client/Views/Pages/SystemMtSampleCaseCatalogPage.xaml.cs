using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Pages
{
    public partial class SystemMtSampleCaseCatalogPage : INavigableView<ViewModels.SystemMtSampleCaseCatalogViewModel>
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
