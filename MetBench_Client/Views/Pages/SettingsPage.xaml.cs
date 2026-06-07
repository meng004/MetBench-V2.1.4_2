using System.Diagnostics;
using System.Windows.Navigation;

namespace MetBench_Client.Views.Pages
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml.
    /// </summary>
    public partial class SettingsPage
    {
        public ViewModels.SettingsViewModel ViewModel
        {
            get;
        }

        public SettingsPage(ViewModels.SettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            //数据上下文初始化赋值
            DataContext = this;
            InitializeComponent();
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true,
            });
            e.Handled = true;
        }
    }
}
