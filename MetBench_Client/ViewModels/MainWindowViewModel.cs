using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MetBench_UI.Localization;
using Wpf.Ui.Controls;
using Wpf.Ui;
using System.Windows.Automation;

namespace MetBench_Client.ViewModels
{
    //主页面VM
    public partial class MainWindowViewModel : ObservableObject
    {
        private bool _isInitialized = false;

        private readonly IAppLocalizationService _localization;
        private readonly List<(NavigationViewItem Item, string Key)> _localizedNavigation = new();
        private readonly List<(NavigationViewItem Item, string Key)> _localizedFooter = new();

        [ObservableProperty]
        private string _applicationTitle = string.Empty;

        [ObservableProperty]
        private ObservableCollection<object> _navigationItems = new();

        [ObservableProperty]
        private ObservableCollection<object> _navigationFooter = new();

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new();

        public string _headerString = string.Empty;

        public LocalizedTextProvider Localization { get; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Style",
            "IDE0060:Remove unused parameter",
            Justification = "Demo"
        )]
        public MainWindowViewModel(INavigationService navigationService, IAppLocalizationService localization, LocalizedTextProvider localizedText)
        {
            _localization = localization;
            Localization = localizedText;
            _localization.CultureChanged += (_, _) => RefreshLocalizedText();
            if (!_isInitialized)
            {
                InitializeViewModel();
            }
        }

        public void RefreshLocalizedText()
        {
            ApplicationTitle = _localization.GetString("App_Title");
            foreach (var pair in _localizedNavigation)
                pair.Item.Content = _localization.GetString(pair.Key);
            foreach (var pair in _localizedFooter)
                pair.Item.Content = _localization.GetString(pair.Key);
            foreach (var item in TrayMenuItems)
                if (Equals(item.Tag, "tray_home"))
                    item.Header = _localization.GetString("Tray_Home");
        }

        private NavigationViewItem LocalizedNav(string key, SymbolRegular symbol, System.Type targetPageType, List<(NavigationViewItem Item, string Key)> registry)
        {
            var item = new NavigationViewItem
            {
                Content = _localization.GetString(key),
                Icon = new SymbolIcon { Symbol = symbol },
                TargetPageType = targetPageType
            };
            AutomationProperties.SetAutomationId(item, key);
            registry.Add((item, key));
            return item;
        }

        private void InitializeViewModel()
        {
            ApplicationTitle = _localization.GetString("App_Title");

            NavigationItems = new ObservableCollection<object>
            {
                LocalizedNav("Nav_MrDisplay",                  SymbolRegular.CalendarDataBar24,       typeof(Views.Pages.MRDisplayPage),                  _localizedNavigation),
                LocalizedNav("Nav_MrManagement",               SymbolRegular.DataHistogram24,         typeof(Views.Pages.MRManagementPage),               _localizedNavigation),
                LocalizedNav("Nav_ApplicationManagement",      SymbolRegular.DataHistogram24,         typeof(Views.Pages.ApplicationManagementPage),      _localizedNavigation),
                LocalizedNav("Nav_DomainManagement",           SymbolRegular.DataHistogram24,         typeof(Views.Pages.DomainManagementPage),           _localizedNavigation),
                LocalizedNav("Nav_MtExecution",                SymbolRegular.PersonRunning20,         typeof(Views.Pages.MTExecutionPage),                _localizedNavigation),
                LocalizedNav("Nav_SystemMtExecution",          SymbolRegular.PlayCircle24,            typeof(Views.Pages.SystemMtExecutionPage),          _localizedNavigation),
                LocalizedNav("Nav_SystemMtAsyncExecution",     SymbolRegular.Timer24,                 typeof(Views.Pages.SystemMtAsyncJobPage),           _localizedNavigation),
                LocalizedNav("Nav_SystemMtMrCatalog",          SymbolRegular.DocumentQueueMultiple24, typeof(Views.Pages.SystemMtMrCatalogPage),          _localizedNavigation),
                LocalizedNav("Nav_SystemMtSutCatalog",         SymbolRegular.Apps24,                  typeof(Views.Pages.SystemMtSutCatalogPage),         _localizedNavigation),
                LocalizedNav("Nav_SystemMtEquationCatalog",    SymbolRegular.MathFormula24,           typeof(Views.Pages.SystemMtEquationCatalogPage),    _localizedNavigation),
                LocalizedNav("Nav_SystemMtSampleCaseCatalog",  SymbolRegular.DocumentText24,          typeof(Views.Pages.SystemMtSampleCaseCatalogPage),  _localizedNavigation),
                LocalizedNav("Nav_SystemMtExecutionHistory",   SymbolRegular.History24,               typeof(Views.Pages.SystemMtExecutionHistoryPage),   _localizedNavigation),
                LocalizedNav("Nav_SystemMtResult",             SymbolRegular.DataBarVertical24,       typeof(Views.Pages.SystemMtResultPage),             _localizedNavigation),
                LocalizedNav("Nav_Anomalies",                  SymbolRegular.Warning24,               typeof(Views.Pages.AnomalyListPage),                _localizedNavigation),
                LocalizedNav("Nav_Discovery",                  SymbolRegular.Lightbulb24,             typeof(Views.Pages.DiscoveryPage),                  _localizedNavigation),
                LocalizedNav("Nav_CandidateReview",            SymbolRegular.ClipboardCheckmark24,    typeof(Views.Pages.CandidateReviewPage),            _localizedNavigation),
                LocalizedNav("Nav_Mutation",                   SymbolRegular.Bug24,                   typeof(Views.Pages.MutationCampaignPage),           _localizedNavigation),
                LocalizedNav("Nav_Replay",                     SymbolRegular.ArrowCounterclockwise24, typeof(Views.Pages.ReplayResultPage),               _localizedNavigation),
                LocalizedNav("Nav_Coverage",                   SymbolRegular.DataPie20,               typeof(Views.Pages.CoverageDashboardPage),          _localizedNavigation),
                LocalizedNav("Nav_MetaPatterns",               SymbolRegular.Shapes24,                typeof(Views.Pages.MetaPatternsPage),               _localizedNavigation),
                LocalizedNav("Nav_MrDetection",                SymbolRegular.CalendarSearch20,        typeof(Views.Pages.AutoDetectMRPage),               _localizedNavigation),
                LocalizedNav("Nav_MrRecommendation",           SymbolRegular.CalendarArrowRight24,    typeof(Views.Pages.MRRecommendationPage),           _localizedNavigation),
                LocalizedNav("Nav_MrReportGenerator",          SymbolRegular.DocumentQueueMultiple24, typeof(Views.Pages.MTReportGeneratorPage),          _localizedNavigation),
            };

            NavigationFooter = new ObservableCollection<object>
            {
                LocalizedNav("Nav_Settings", SymbolRegular.Settings24, typeof(Views.Pages.SettingsPage), _localizedFooter)
            };

            TrayMenuItems = new ObservableCollection<MenuItem>()
            {
                new MenuItem()
                {
                    Header = _localization.GetString("Tray_Home"),
                    Tag = "tray_home"
                }
            };

            _isInitialized = true;
        }
    }
}
