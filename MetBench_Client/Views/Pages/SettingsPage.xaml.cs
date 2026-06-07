using System.Diagnostics;
using System.Windows;
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
            DataContext = this;
            Loaded += SettingsPage_Loaded;
            Unloaded += SettingsPage_Unloaded;
            InitializeComponent();
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.OnNavigatedTo();
        }

        private void SettingsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.OnNavigatedFrom();
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
