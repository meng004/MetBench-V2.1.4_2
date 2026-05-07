using MetBench_Client.ViewModels;
using MetBench_Domain;
using Prism.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Pages
{
    /// <summary>
    /// MRManagementPage.xaml 的交互逻辑
    /// </summary>
    public partial class MRRecommendationPage : Page, INavigableView<ViewModels.MRRecommendationViewModel>
    {
        public MRRecommendationPage(ViewModels.MRRecommendationViewModel viewModel)
        {
            ViewModel = viewModel;
            //数据上下文初始化赋值
            DataContext = this;
            InitializeComponent();
        }

        public MRRecommendationViewModel ViewModel
        {
            get;
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
