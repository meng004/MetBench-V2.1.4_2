using MetBench_Client.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Windows
{
    /// <summary>
    /// ApplicationProgramsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ApplicationProgramsWindow : INavigableView<ViewModels.ApplicationManagementViewModel>
    {
        public ApplicationManagementViewModel ViewModel
        {
            get;
        }

        public ApplicationProgramsWindow(ViewModels.ApplicationManagementViewModel viewModel)
        {
            ViewModel = viewModel;
            //数据上下文初始化赋值
            DataContext = this;
            InitializeComponent();
        }

        public void ShowWindow()
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Show();
        }

        public void CloseWindow()
        {
            // 将窗口隐藏
            this.Hide();
        } /*=> Close();*/
    }
}
