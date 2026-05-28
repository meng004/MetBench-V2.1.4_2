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
using MetBench_BLL.SystemMT.Bootstrap;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Metadata;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Catalog.Editing;
using MetBench_BLL.SystemMT.Reporting;
using MetBench_BLL.Discovery;
using MetBench_BLL.Discovery.Validators;
using MetBench_BLL.Mutation;
using MetBench_BLL.Coverage;
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
            .ConfigureAppConfiguration(c =>
            {
                c.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location));
                c.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false);
            })
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
                services.AddScoped<MetamorphicRelationService>();
                services.AddScoped<ApplicationService>();
                services.AddScoped<DomainService>();

                // 蜕变测试执行
                services.AddScoped<AutoRunMR_Await>();

                // System-level metamorphic testing
                services.AddScoped<CliProgramRunner>();
                services.AddScoped(provider => new PythonOutputAdapter(
                    OperatingSystem.IsWindows() ? "python" : "python3"));
                services.AddScoped(provider => new PythonInputAdapter(
                    OperatingSystem.IsWindows() ? "python" : "python3"));
                // InputGenerator needs an adapter script path; register a factory the
                // caller can override per-task in Stage 4. Stage 2 leaves the path
                // resolution to whoever resolves InputGenerator at the call site.
                services.AddScoped<InputGenerator>(provider =>
                    throw new InvalidOperationException(
                        "InputGenerator must be constructed with a per-task adapter path; resolve PythonInputAdapter and the adapter path from the task instead."));
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

                // P3.3 — launcher 经 SystemMtPipeline + SystemMtExecutionRecorder 落
                // Execution+Result+Anomaly。lifetime 改 Scoped 与 IExecutionRepository /
                // IResultRepository / ISystemMtPipeline 一致。
                services.AddScoped<SystemMtExecutionRecorder>();
                services.AddSingleton<IMrCatalogProvider>(provider =>
                    new ManifestMrCatalogProvider(
                        provider.GetRequiredService<LauncherOptions>()));
                services.AddSingleton<ISystemMtManifestCatalogEditor>(provider =>
                    new SystemMtManifestCatalogEditor(
                        provider.GetRequiredService<LauncherOptions>().SutRoot));
                services.AddSingleton<ISystemMtSutEditor>(provider =>
                    new SystemMtSutEditor(
                        provider.GetRequiredService<LauncherOptions>().SutRoot));
                services.AddScoped<ISystemMtLauncher, SystemMtLauncher>();
                services.AddScoped<ISystemMtCatalogReader>(provider =>
                    (ISystemMtCatalogReader)provider.GetRequiredService<ISystemMtLauncher>());
                services.AddSingleton<ISystemMtResultReportRenderer, HtmlSystemMtResultReportRenderer>();

                services.AddScoped<Views.Pages.SystemMtExecutionPage>();
                services.AddScoped<ViewModels.SystemMtExecutionViewModel>();
                services.AddScoped<Views.Pages.SystemMtMrCatalogPage>();
                services.AddScoped<ViewModels.SystemMtMrCatalogViewModel>();
                services.AddScoped<Views.Pages.SystemMtSutCatalogPage>();
                services.AddScoped<ViewModels.SystemMtSutCatalogViewModel>();
                services.AddSingleton<ISystemMtSampleCaseEditor>(provider =>
                    new SystemMtSampleCaseEditor(
                        provider.GetRequiredService<LauncherOptions>().SutRoot));
                services.AddScoped<Views.Pages.SystemMtSampleCaseCatalogPage>();
                services.AddScoped<ViewModels.SystemMtSampleCaseCatalogViewModel>();

                // === v2 SystemMT repositories (LiteDB) + Anomaly stack ===
                services.AddSystemMtRepositories();
                services.AddScoped<IAnomalyService, AnomalyService>();

                // === G-08b: metadata catalog DB + bootstrap ===
                services.AddSingleton<ISystemMtMetadataRepository>(provider =>
                {
                    var dataDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
                    return new LiteDbSystemMtMetadataRepository(
                        $"Filename={Path.Combine(dataDir, "MetadataSystemMT.Litedb")}");
                });
                services.AddScoped<LauncherCatalogV2Importer>(provider =>
                    new LauncherCatalogV2Importer(
                        provider.GetRequiredService<ISystemMtCatalogReader>(),
                        provider.GetRequiredService<IApplicationRepository>(),
                        provider.GetRequiredService<IMetamorphicRelationRepository>(),
                        provider.GetRequiredService<IMRBindingRepository>(),
                        provider.GetRequiredService<IAuditLogRepository>()));

                // === v2 Anomaly list page ===
                services.AddScoped<Views.Pages.AnomalyListPage>();
                services.AddScoped<ViewModels.AnomalyListViewModel>();

                // === v2 Replay result page (§1.2) ===
                services.AddSingleton<MetBench_Client.Services.ReplayInbox>();
                services.AddScoped<ISystemMtPipeline, SystemMtPipeline>();
                services.AddScoped<ReplayService>();
                services.AddScoped<ReplayContextBuilder>();
                services.AddScoped<Views.Pages.ReplayResultPage>();
                services.AddScoped<ViewModels.ReplayResultViewModel>();

                // 结果可视化相关IOC配置
                services.AddScoped<MTVisualizationService>();

                // === v2 Discovery stack (P7) ===
                services.AddScoped<DiscoveryService>();
                services.AddSingleton<ILlmGateway>(provider =>
                {
                    var cfg = provider.GetRequiredService<IConfiguration>();
                    var baseUrl = cfg["DeepSeek:BaseUrl"];
                    var apiKey  = cfg["DeepSeek:ApiKey"];
                    var model   = cfg["DeepSeek:Model"];
                    if (!string.IsNullOrWhiteSpace(baseUrl) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(model))
                        return new DeepSeekLlmGateway(baseUrl, apiKey, model);
                    return new NullLlmGateway();
                });
                services.AddSingleton<MetaPatternDiscoverer>(provider => new MetaPatternDiscoverer(
                    OperatingSystem.IsWindows() ? "python" : "python3",
                    Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!, "tools", "noether_candidates.py")));
                services.AddSingleton<LlmNativeDiscoverer>(provider =>
                    new LlmNativeDiscoverer(provider.GetRequiredService<ILlmGateway>()));
                services.AddSingleton<IMRDiscoverer>(provider => provider.GetRequiredService<MetaPatternDiscoverer>());
                services.AddSingleton<IMRDiscoverer>(provider => provider.GetRequiredService<LlmNativeDiscoverer>());
                services.AddScoped<Views.Pages.DiscoveryPage>();
                services.AddScoped<ViewModels.DiscoveryViewModel>();

                // === v2 Validation stack (P7) — T-C: 真实 sampler 替换 hardcoded stub ===
                services.AddScoped<ValidationService>();
                services.AddScoped<EmpiricalRepoSampler>();
                services.AddScoped<EmpiricalValidator>(provider =>
                    new EmpiricalValidator(provider.GetRequiredService<EmpiricalRepoSampler>().SampleAsync));
                services.AddScoped<TheoreticalLlmValidator>(provider =>
                    new TheoreticalLlmValidator(provider.GetRequiredService<ILlmGateway>()));
                // AdversarialMutmutValidator + AdversarialCampaignSampler 已删除（next-stage P0
                // 模型对齐：MR 识别收敛为 meta-prompt + multi-LLM 共识，T6 变异由 MutationCampaign 子系统承担）
                services.AddScoped<Views.Pages.CandidateReviewPage>();
                services.AddScoped<ViewModels.CandidateReviewViewModel>();

                // === v2 Mutation campaign (P7) ===
                services.AddScoped<MutationCampaignService>();
                services.AddScoped<Views.Pages.MutationCampaignPage>();
                services.AddScoped<ViewModels.MutationCampaignViewModel>();

                // === v2 Coverage dashboard (P8) ===
                services.AddScoped<CoverageService>();
                services.AddScoped<Views.Pages.CoverageDashboardPage>();
                services.AddScoped<ViewModels.CoverageDashboardViewModel>();

                // Trend dashboard 已删除（next-stage P0：Trend 子系统下线）。

                // === v2 MetaPatterns CRUD (F21 / PR-VM-3) ===
                services.AddScoped<Views.Pages.MetaPatternsPage>();
                services.AddScoped<ViewModels.MetaPatternsViewModel>();

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
                services.AddScoped<MRRecommendationService>();

                // 蜕变测试报告生成相关IOC配置
                services.AddScoped<MTReportGeneratorService>();

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

            // G-08b: idempotent catalog seed on every startup
            var metaRepo = _host.Services.GetService<ISystemMtMetadataRepository>();
            var importer = _host.Services.GetService<LauncherCatalogV2Importer>();
            await SystemMtBootstrap.SeedCatalogsAsync(metaRepo, importer);
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
            var logPath = Path.Combine(Path.GetTempPath(), "MetBench_crash.log");
            try { File.WriteAllText(logPath, $"{DateTime.Now}\n{e.Exception}"); } catch { }
            e.Handled = true;
            System.Windows.MessageBox.Show(
                $"Unhandled exception:\n{e.Exception.GetType().Name}: {e.Exception.Message}\n\nSee {logPath} for full details.",
                "MetBench Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
