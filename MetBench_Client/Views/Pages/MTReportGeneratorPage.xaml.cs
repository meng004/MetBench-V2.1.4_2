using MetBench_Client.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Pages
{
    /// <summary>
    /// MTExecutionPage.xaml 的交互逻辑
    /// </summary>
    public partial class MTReportGeneratorPage : Page,INavigableView<ViewModels.MTReportGeneratorViewModel>
    {
        public MTReportGeneratorViewModel ViewModel
        {
            get;
        }
        public MTReportGeneratorPage(ViewModels.MTReportGeneratorViewModel viewModel)
        {
            ViewModel = viewModel;
            //数据上下文初始化赋值
            DataContext = this;
            InitializeComponent();
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
