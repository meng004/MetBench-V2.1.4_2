using System.Windows.Controls;

namespace MetBench_Client.Views.Pages
{
    public partial class SystemMtExecutionHistoryPage
    {
        public ViewModels.SystemMtExecutionHistoryViewModel ViewModel { get; }

        public SystemMtExecutionHistoryPage(ViewModels.SystemMtExecutionHistoryViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
            DataGrid_SystemMtExecutionHistory.SelectionChanged += OnDataGridSelectionChanged;
        }

        private void OnDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // SelectionMode=Extended carries multi-row state in DataGrid.SelectedItems; mirror it
            // into the VM so the Delete command sees the live set without VM probing the view.
            if (DataGrid_SystemMtExecutionHistory.SelectedItems is null) return;
            ViewModel.UpdateSelectedRowsFromView(DataGrid_SystemMtExecutionHistory.SelectedItems);
        }
    }
}
