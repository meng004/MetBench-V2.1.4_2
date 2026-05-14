using MetBench_Client.Models;
using MetBench_Client.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using MetBench_DAL;
using MetBench_IDAL;
using MetBench_BLL;
using MetBench_BLL.SystemMT;
using MetBench_BLL.SystemMT.Anomaly;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;
using Wpf.Ui.Controls;
using Wpf.Ui;
using Stylet;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace MetBench_Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        //主机构建和配置部分
        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)); })
            .ConfigureServices((context, services) =>
            {  
                //添加各种服务和页面
                // App Host
                services.AddHostedService<ApplicationHostService>();

                // Page resolver service 注入服务提供程序
                services.AddSingleton<IPageService, PageService>();

                // Theme manipulation
                services.AddSingleton<IThemeService, ThemeService>();

                // TaskBar manipulation
                services.AddSingleton<ITaskBarService, TaskBarService>();

                // Service containing navigation, same as INavigationWindow... but without window
                services.AddSingleton<INavigationService, NavigationService>();

                // Main window with navigation
                services.AddScoped<INavigationWindow, Views.Windows.MainWindow>();
                services.AddScoped< Views.Windows.ApplicationProgramsWindow>();
                services.AddScoped<ViewModels.MainWindowViewModel>();

                // Views and ViewModels  将Page和ViewModel加入服务
                services.AddScoped<Views.Pages.DashboardPage>();
                services.AddScoped<ViewModels.DashboardViewModel>();
                services.AddScoped<Views.Pages.DataPage>();
                services.AddScoped<ViewModels.DataViewModel>();
                services.AddScoped<Views.Pages.SettingsPage>();
                services.AddScoped<ViewModels.SettingsViewModel>();

                services.AddScoped<Views.Pages.MRDisplayPage>();
                services.AddScoped<ViewModels.MRDisplayViewModel>();
                services.AddScoped<Views.Pages.MRManagementPage>();
                services.AddScoped<ViewModels.MRManagementViewModel>();
                services.AddScoped<Views.Pages.ApplicationManagementPage>();
                services.AddScoped<ViewModels.ApplicationManagementViewModel>();
                services.AddScoped<Views.Pages.DomainManagementPage>();
                services.AddScoped<ViewModels.DomainManagementViewModel>();
                services.AddScoped<Views.Pages.MTExecutionPage>();
                services.AddScoped<ViewModels.MTExecutionViewModel>();
                services.AddScoped<Views.Pages.AutoDetectMRPage>();
                services.AddScoped<ViewModels.AutoDetectMRViewModel>();
                services.AddScoped<Views.Pages.MRRecommendationPage>();
                services.AddScoped<ViewModels.MRRecommendationViewModel>();
                services.AddScoped<Views.Pages.MTReportGeneratorPage>();
                services.AddScoped<ViewModels.MTReportGeneratorViewModel>();

                // Configuration
                services.Configure<AppConfig>(context.Configuration.GetSection(nameof(AppConfig)));

                // 注册数据仓库
                services.AddSingleton<IApplicationRepository, ApplicationRepository>();
                services.AddSingleton<IMetamorphicRelationRepository, MetamorphicRelationRepository>();
                services.AddSingleton<IDomainRepository, DomainRepository>();

                // 注册业务类
                services.AddScoped<MetamorphicRelationSerive>();
                services.AddScoped<ApplicationSerive>();
                services.AddScoped<DomainSerive>();

                // 蜕变测试执行
                services.AddScoped<AutoRunMR_Await>();

                // System-level metamorphic testing
                services.AddScoped<CliProgramRunner>();
                services.AddScoped(provider => new PythonOutputAdapter(
                    OperatingSystem.IsWindows() ? "python" : "python3"));
                services.AddScoped(provider => new PythonInputAdapter(
                    OperatingSystem.IsWindows() ? "python" : "python3"));
                services.AddScoped<IMrAssertion, GreaterThanAssertion>();
                services.AddScoped<IMrAssertion, LessThanAssertion>();
                // InputGenerator needs an adapter script path; register a factory the
                // caller can override per-task in Stage 4. Stage 2 leaves the path
                // resolution to whoever resolves InputGenerator at the call site.
                services.AddScoped<InputGenerator>(provider =>
                    throw new InvalidOperationException(
                        "InputGenerator must be constructed with a per-task adapter path; resolve PythonInputAdapter and the adapter path from the task instead."));
                services.AddScoped<SystemMtRunner>();

                // Stage 4 launcher facade + persistence + reporting
                services.AddSingleton(provider => new LauncherOptions(
                    SutRoot: Path.Combine(
                        Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!,
                        "SUT"),
                    SystemPython: OperatingSystem.IsWindows() ? "python" : "python3",
                    OpenMocPython: Environment.GetEnvironmentVariable("METBENCH_OPENMOC_PYTHON")
                        ?? (OperatingSystem.IsWindows() ? "python" : "python3")));

                services.AddSingleton<ISystemMtResultRepository>(provider =>
                {
                    var dataDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
                    var dbPath = Path.Combine(dataDir, "SystemMT.Litedb");
                    return new LiteDbSystemMtResultRepository($"Filename={dbPath}");
                });

                services.AddSingleton<ISystemMtScenarioLauncher, SystemMtScenarioLauncher>();
                services.AddSingleton<ISystemMtResultReportRenderer, HtmlSystemMtResultReportRenderer>();

                services.AddScoped<Views.Pages.SystemMtExecutionPage>();
                services.AddScoped<ViewModels.SystemMtExecutionViewModel>();

                // === v2 SystemMT repositories (LiteDB) + Anomaly stack ===
                services.AddSystemMtRepositories();
                services.AddScoped<IAnomalyService, AnomalyService>();

                // === v2 Anomaly list page ===
                services.AddScoped<Views.Pages.AnomalyListPage>();
                services.AddScoped<ViewModels.AnomalyListViewModel>();

                // 结果可视化相关IOC配置
                services.AddScoped<MTVisualizationSerive>();

                // 蜕变关系识别相关IOC配置
                services.AddScoped<MRDetector>();
                services.AddScoped<IRecognitionAlgorithm, AutoMRAlgorithm>();
                services.AddScoped<IResultParser, AutoMRParser>();

                // 蜕变关系推荐相关IOC配置
                services.AddScoped<SyntaxMRRecommendation>(); // 语法推荐服务
                services.AddScoped<SemanticMRRecommendation>();// 语义推荐服务
                services.AddScoped<IPreprocessor, UnitLevelPreprocessor>(); // 单元级别预处理

                services.AddScoped<ISyntaxSimilarityDetector, SyntaxSimilarityDetector>();// 语法相似检测
                services.AddScoped<ISemanticSimilarityDetector, SemanticSimilarityDetector>();// 语义相似检测
                services.AddScoped<SimilarityResultFilter>();
                services.AddScoped<SupportRateCalculator>();
                services.AddScoped<MRRecommendationSerive>();

                // 蜕变测试报告生成相关IOC配置
                services.AddScoped<MTReportGeneratorSerive>();

                // 注册 IEventAggregator
                services.AddSingleton<IEventAggregator, EventAggregator>();

            }).Build();

        /// <summary>
        /// Gets registered service.
        /// 实现了依赖注入和代码重用。
        /// </summary>
        /// <typeparam name="T">Type of the service to get.</typeparam>
        /// <returns>Instance of the service or <see langword="null"/>.</returns>
        public static T GetService<T>()
            where T : class
        {
            return _host.Services.GetService(typeof(T)) as T;
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            await _host.StartAsync();
        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();

            _host.Dispose();
            string tempPath = Path.GetTempPath();
            string metBenchPath = Path.Combine(tempPath, "MetBench");
            string tempImagePath = Path.Combine(tempPath, "temp_image");

            if (Directory.Exists(metBenchPath))
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = $"/C ping 127.0.0.1 -n 2 > nul & rmdir /s /q \"{metBenchPath}\"";
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.CreateNoWindow = true;
                    Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("启动删除MetBench进程时发生错误：" + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("MetBench文件夹不存在，无需删除！");
            }

            if (Directory.Exists(tempImagePath))
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = $"/C ping 127.0.0.1 -n 2 > nul & rmdir /s /q \"{tempImagePath}\"";
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.CreateNoWindow = true;
                    Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("启动删除temp_image进程时发生错误：" + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("temp_image文件夹不存在，无需删除！");
            }
        }

        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
        }
    }
}