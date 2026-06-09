using CommunityToolkit.Mvvm.ComponentModel;
using MetBench_Client.Models;
using MetBench_Client.Services;
using MetBench_UI.Localization;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MetBench_Client.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IAppLocalizationService _localization;
        private readonly List<NavigationItem> _localizedNavigation = new();
        private readonly List<NavigationItem> _localizedFooter = new();
        private bool _isInitialized;

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

        public LocalizedTextProvider Localization
        {
            get;
        }

        [ObservableProperty]
        private string _applicationTitle = string.Empty;

        [ObservableProperty]
        private ObservableCollection<NavigationItem> _navigationItems = new();

        [ObservableProperty]
        private ObservableCollection<NavigationItem> _navigationFooter = new();

        public void RefreshLocalizedText()
        {
            ApplicationTitle = _localization.GetString("App_Title");
            foreach (var item in _localizedNavigation)
            {
                item.Content = _localization.GetString(item.Key);
            }

            foreach (var item in _localizedFooter)
            {
                item.Content = _localization.GetString(item.Key);
            }
        }

        private NavigationItem LocalizedNav(string key, System.Type targetPageType, List<NavigationItem> registry)
        {
            var item = new NavigationItem(key, _localization.GetString(key), targetPageType);
            registry.Add(item);
            return item;
        }

        private void InitializeViewModel()
        {
            ApplicationTitle = _localization.GetString("App_Title");

            NavigationItems = new ObservableCollection<NavigationItem>
            {
                LocalizedNav("Nav_MrDisplay", typeof(Views.Pages.MRDisplayPage), _localizedNavigation),
                LocalizedNav("Nav_MrManagement", typeof(Views.Pages.MRManagementPage), _localizedNavigation),
                LocalizedNav("Nav_ApplicationManagement", typeof(Views.Pages.ApplicationManagementPage), _localizedNavigation),
                LocalizedNav("Nav_DomainManagement", typeof(Views.Pages.DomainManagementPage), _localizedNavigation),
                LocalizedNav("Nav_MtExecution", typeof(Views.Pages.MTExecutionPage), _localizedNavigation),
                LocalizedNav("Nav_SystemMtExecution", typeof(Views.Pages.SystemMtExecutionPage), _localizedNavigation),
                LocalizedNav("Nav_SystemMtAsyncExecution", typeof(Views.Pages.SystemMtAsyncJobPage), _localizedNavigation),
                LocalizedNav("Nav_SystemMtMrCatalog", typeof(Views.Pages.SystemMtMrCatalogPage), _localizedNavigation),
                LocalizedNav("Nav_SystemMtSutCatalog", typeof(Views.Pages.SystemMtSutCatalogPage), _localizedNavigation),
                LocalizedNav("Nav_SystemMtEquationCatalog", typeof(Views.Pages.SystemMtEquationCatalogPage), _localizedNavigation),
                LocalizedNav("Nav_SystemMtSampleCaseCatalog", typeof(Views.Pages.SystemMtSampleCaseCatalogPage), _localizedNavigation),
                LocalizedNav("Nav_SystemMtExecutionHistory", typeof(Views.Pages.SystemMtExecutionHistoryPage), _localizedNavigation),
                LocalizedNav("Nav_SystemMtResult", typeof(Views.Pages.SystemMtResultPage), _localizedNavigation),
                LocalizedNav("Nav_Anomalies", typeof(Views.Pages.AnomalyListPage), _localizedNavigation),
                LocalizedNav("Nav_Discovery", typeof(Views.Pages.DiscoveryPage), _localizedNavigation),
                LocalizedNav("Nav_CandidateReview", typeof(Views.Pages.CandidateReviewPage), _localizedNavigation),
                LocalizedNav("Nav_Mutation", typeof(Views.Pages.MutationCampaignPage), _localizedNavigation),
                LocalizedNav("Nav_Replay", typeof(Views.Pages.ReplayResultPage), _localizedNavigation),
                LocalizedNav("Nav_Coverage", typeof(Views.Pages.CoverageDashboardPage), _localizedNavigation),
                LocalizedNav("Nav_MetaPatterns", typeof(Views.Pages.MetaPatternsPage), _localizedNavigation),
                LocalizedNav("Nav_MrDetection", typeof(Views.Pages.AutoDetectMRPage), _localizedNavigation),
                LocalizedNav("Nav_MrRecommendation", typeof(Views.Pages.MRRecommendationPage), _localizedNavigation),
                LocalizedNav("Nav_MrReportGenerator", typeof(Views.Pages.MTReportGeneratorPage), _localizedNavigation),
            };

            NavigationFooter = new ObservableCollection<NavigationItem>
            {
                LocalizedNav("Nav_Settings", typeof(Views.Pages.SettingsPage), _localizedFooter),
            };

            _isInitialized = true;
        }
    }
}
