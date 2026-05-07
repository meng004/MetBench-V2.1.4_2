 using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using MetBench_Client.ViewModels;

namespace MetBench_Client.Views.Pages
{
    /// <summary>
    /// ApplicationManagementPage.xaml 的交互逻辑
    /// </summary>
    public partial class ApplicationManagementPage : Page, INavigableView<ViewModels.ApplicationManagementViewModel>
    {
        public ApplicationManagementViewModel ViewModel
        {
            get;
        }
        public ApplicationManagementPage(ViewModels.ApplicationManagementViewModel viewModel)
        {
            ViewModel = viewModel;
            //数据上下文初始化赋值
            DataContext = this;
            InitializeComponent();
        }

        //记录DataGrid行数
        private void dataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {

            e.Row.Header = e.Row.GetIndex() + 1;

        }
        //TextBox获得焦点的行为
        private void textBox_GotFocus(object sender, RoutedEventArgs e)
        {
            Wpf.Ui.Controls.TextBox textBox = (Wpf.Ui.Controls.TextBox)sender;

            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.Height = double.NaN; // Set the height to auto-expand
            textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto; // Show vertical scrollbar if needed
        }
        //TextBox失去焦点的行为
        private void textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Wpf.Ui.Controls.TextBox textBox = (Wpf.Ui.Controls.TextBox)sender;

            textBox.TextWrapping = TextWrapping.NoWrap; // Restore original text wrapping
            textBox.Height = double.NaN; // Set the height back to auto
            textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden; // Hide vertical scrollbar
        }
    }
}
