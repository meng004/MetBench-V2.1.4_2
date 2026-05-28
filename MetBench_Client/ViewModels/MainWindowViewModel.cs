using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Wpf.Ui.Controls;
using Wpf.Ui;

namespace MetBench_Client.ViewModels
{
    //主页面VM
    public partial class MainWindowViewModel : ObservableObject
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _applicationTitle = string.Empty;

        [ObservableProperty]
        private ObservableCollection<object> _navigationItems = new();

        [ObservableProperty]
        private ObservableCollection<object> _navigationFooter = new();

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new();

        public string _headerString = string.Empty;

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Style",
            "IDE0060:Remove unused parameter",
            Justification = "Demo"
        )]
        public MainWindowViewModel(INavigationService navigationService)
        {
            if (!_isInitialized)
            {
                InitializeViewModel();
            }
        }

        private void InitializeViewModel()
        {
            //ApplicationTitle = "数值表达式型蜕变关系的存储管理系统";
            //ApplicationTitle = "Numerical Expression Metamorphic Relations Repository";
            ApplicationTitle = "MetBench";
            //ApplicationTitle = "MetBench: A Numerical Expression Metamorphic Relations Benchmark Dataset";

            NavigationItems = new ObservableCollection<object>
            {

             new NavigationViewItem()
            {
                Content = "MR Display",
                Icon = new SymbolIcon { Symbol = SymbolRegular.CalendarDataBar24 },
                TargetPageType = typeof(Views.Pages.MRDisplayPage)
            },
              new NavigationViewItem()
            {
                Content = "MR Management",
                Icon = new SymbolIcon { Symbol = SymbolRegular.DataHistogram24 },
                TargetPageType = typeof(Views.Pages.MRManagementPage)
            },
               new NavigationViewItem()
            {
                Content = "Application Management",
                Icon = new SymbolIcon { Symbol = SymbolRegular.DataHistogram24 },
                TargetPageType = typeof(Views.Pages.ApplicationManagementPage)
            },
                new NavigationViewItem()
            {
                Content = "Domain Management",
                Icon = new SymbolIcon { Symbol = SymbolRegular.DataHistogram24 },
                TargetPageType = typeof(Views.Pages.DomainManagementPage)
            },
                     new NavigationViewItem()
            {
                Content = "MT Execution",
                Icon = new SymbolIcon { Symbol = SymbolRegular.PersonRunning20 },
                TargetPageType = typeof(Views.Pages.MTExecutionPage)
            },
                     new NavigationViewItem()
            {
                Content = "System MT",
                Icon = new SymbolIcon { Symbol = SymbolRegular.PlayCircle24 },
                TargetPageType = typeof(Views.Pages.SystemMtExecutionPage)
            },
                     new NavigationViewItem()
            {
                Content = "System MT MR Catalog",
                Icon = new SymbolIcon { Symbol = SymbolRegular.DocumentQueueMultiple24 },
                TargetPageType = typeof(Views.Pages.SystemMtMrCatalogPage)
            },
                     new NavigationViewItem()
            {
                Content = "System MT SUT Catalog",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Apps24 },
                TargetPageType = typeof(Views.Pages.SystemMtSutCatalogPage)
            },
                     new NavigationViewItem()
            {
                Content = "System MT Equation Catalog",
                Icon = new SymbolIcon { Symbol = SymbolRegular.MathFormula24 },
                TargetPageType = typeof(Views.Pages.SystemMtEquationCatalogPage)
            },
                     new NavigationViewItem()
            {
                Content = "System MT Sample Case Catalog",
                Icon = new SymbolIcon { Symbol = SymbolRegular.DocumentText24 },
                TargetPageType = typeof(Views.Pages.SystemMtSampleCaseCatalogPage)
            },
                     new NavigationViewItem()
            {
                Content = "Anomalies",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Warning24 },
                TargetPageType = typeof(Views.Pages.AnomalyListPage)
            },
                     new NavigationViewItem()
            {
                Content = "Discovery",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Lightbulb24 },
                TargetPageType = typeof(Views.Pages.DiscoveryPage)
            },
                     new NavigationViewItem()
            {
                Content = "Candidate Review",
                Icon = new SymbolIcon { Symbol = SymbolRegular.ClipboardCheckmark24 },
                TargetPageType = typeof(Views.Pages.CandidateReviewPage)
            },
                     new NavigationViewItem()
            {
                Content = "Mutation",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Bug24 },
                TargetPageType = typeof(Views.Pages.MutationCampaignPage)
            },
                     new NavigationViewItem()
            {
                Content = "Replay",
                Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowCounterclockwise24 },
                TargetPageType = typeof(Views.Pages.ReplayResultPage)
            },
                     new NavigationViewItem()
            {
                Content = "Coverage",
                Icon = new SymbolIcon { Symbol = SymbolRegular.DataPie20 },
                TargetPageType = typeof(Views.Pages.CoverageDashboardPage)
            },
                     new NavigationViewItem()
            {
                Content = "MetaPatterns",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Shapes24 },
                TargetPageType = typeof(Views.Pages.MetaPatternsPage)
            },
                     new NavigationViewItem()
            {
                Content = "MR Detection",
                Icon = new SymbolIcon { Symbol = SymbolRegular.CalendarSearch20 },
                TargetPageType = typeof(Views.Pages.AutoDetectMRPage)
            },
                       new NavigationViewItem()
            {
                Content = "MR Recommendation",
                Icon = new SymbolIcon { Symbol = SymbolRegular.CalendarArrowRight24 },
                TargetPageType = typeof(Views.Pages.MRRecommendationPage)
            },
                        new NavigationViewItem()
            {
                Content = "MR ReportGenerator",
                Icon = new SymbolIcon { Symbol = SymbolRegular.DocumentQueueMultiple24 },
                TargetPageType = typeof(Views.Pages.MTReportGeneratorPage)
            }
            };

            NavigationFooter = new ObservableCollection<object>
            {
                new NavigationViewItem()
                {
                    Content = "Settings",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                    TargetPageType = typeof(Views.Pages.SettingsPage)
                }
            };



            TrayMenuItems = new ObservableCollection<MenuItem>()
            {
                new MenuItem()
                {
                    Header = "Home",
                    Tag = "tray_home"
                }
            };

            _isInitialized = true;
        }
    }
}
