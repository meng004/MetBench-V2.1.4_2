using MetBench_Client.Services;
using MetBench_Client.ViewModels;
using System.Windows;

namespace MetBench_Client.Views.Windows
{
    /// <summary>
    /// ApplicationProgramsWindow.xaml interaction logic.
    /// </summary>
    public partial class ApplicationProgramsWindow : Window, IClientWindow
    {
        public ApplicationProgramsWindow(ApplicationManagementViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }

        public ApplicationManagementViewModel ViewModel
        {
            get;
        }

        public void ShowWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Show();
        }

        public void CloseWindow()
        {
            Hide();
        }
    }
}
