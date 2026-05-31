using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_UI.Localization;

namespace MetBench_Client.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        public LocalizedTextProvider Localization { get; }

        public DashboardViewModel(LocalizedTextProvider localization)
        {
            Localization = localization;
        }

        [ObservableProperty]
        private int _counter = 0;

        [RelayCommand]
        private void OnCounterIncrement()
        {
            Counter++;
        }
    }
}
