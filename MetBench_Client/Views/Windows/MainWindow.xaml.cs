using MetBench_Client.Models;
using MetBench_Client.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace MetBench_Client.Views.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window, IClientNavigationWindow
    {
        private readonly INavigationService _navigationService;

        public MainWindow(ViewModels.MainWindowViewModel viewModel, INavigationService navigationService)
        {
            ViewModel = viewModel;
            _navigationService = navigationService;
            DataContext = this;
            InitializeComponent();
            _navigationService.SetNavigationFrame(ContentFrame);
        }

        public ViewModels.MainWindowViewModel ViewModel
        {
            get;
        }

        public bool Navigate(Type pageType)
        {
            return _navigationService.Navigate(pageType);
        }

        public void ShowWindow()
        {
            Show();
        }

        public void CloseWindow()
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }

        private void NavigationList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox listBox || listBox.SelectedItem is not NavigationItem item)
            {
                return;
            }

            if (ReferenceEquals(listBox, NavigationList))
            {
                FooterNavigationList.SelectedItem = null;
            }
            else
            {
                NavigationList.SelectedItem = null;
            }

            Navigate(item.TargetPageType);
        }
    }
}
