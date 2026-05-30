using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using MetBench_BLL;
using MetBench_Client.Helpers;
using MetBench_Client.Views.Pages;
using MetBench_Domain;
using MetBench_IDAL;
using MetBench_UI.Localization;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Wpf.Ui;
using Wpf.Ui.Controls;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace MetBench_Client.ViewModels
{
    public class MTExecutionViewModel : ObservableObject, INavigationAware
    {
        // 导航服务 用于页面切换
        private INavigationService _navigationService;

        // 页面服务 用于获取页面实体
        private IPageService _pageService;

        // 应用程序管理服务
        private ApplicationService _applicationSerive;

        // 蜕变关系管理服务
        private MetamorphicRelationService _metamorphicRelationSerive;

        // DataGrid的SelectedItem数据绑定属性
        public MetamorphicRelations_QueryResultData DataGridSelectedItem { get; set; }

        // 输入模式的Latex格式
        public String InputPatternSympy { get; set; } = string.Empty;

        // 输出模式的Latex格式
        public String OutputPatternSympy { get; set; } = string.Empty;

        // 输入模式的图片
        public BitmapImage InputPatternImag { get; set; }

        // 输出模式的图片
        public BitmapImage OutputPatternImag { get; set; }

        // 蜕变关系的图片路径
        public string Imagepath { get; set; }

        public BitmapImage Image { get; set; }

        private MetamorphicRelation mr;

        // 被测应用程序
        public MetBench_Domain.Application Application { get; set; }

        // 被测应用程序集
        public ObservableCollection<MetBench_Domain.Application> Applications { get; set; }

        // 可选程序列表
        public ObservableCollection<string> CodeNameOptions { get; set; }

        // CodeName的ComboBox的SelectedIndex属性
        public int SelectedIndex { get; set; }

        // CodeName的ComboBox的SelectedValue属性
        public string SelectedValue { get; set; }

        // TextBox的可见性
        public Visibility IsTextBoxVisible { get; set; }

        // ComboBox的可见性
        public Visibility IsComboBoxVisible { get; set; }

        // 程序名
        public string CodeName { get; set; }

        // 最小值
        public string MinParam { get; set; } = String.Empty;

        // 最大值
        public string MaxParam { get; set; } = String.Empty;

        // 执行次数
        public string ExecutNumber { get; set; } = String.Empty;

        // 阈值 需要转换为double类型 double threshold = double.Parse(Threshold);
        public string Threshold { get; set; } = "1e-4";

        // 输入维度
        public string DimensionOfInputPattern { get; set; }

        // 输出维度
        public string DimensionOfOutputPattern { get; set; }

        // 通过比率
        public string Passrate { get; set; } = String.Empty;

        // 失败比率
        public string Failurerate { get; set; } = String.Empty;

        // 测试时间
        public string TestTime { get; set; } = String.Empty;

        // OrderOfMR_ComboBox的SelectedIndex
        // 0表示PiePlot 1表示ScatterPlotter  2表示LinePlotter
        public int PlotTypeComboBoxSelectedIndex { get; set; } = 0;

        // OrderOfMR_ComboBox的SelectedValue
        public string PlotTypeComboBoxSelectedValue { get; set; } = string.Empty;

        public PlotType SelectedType { get; set; } = PlotType.Pie;

        public List<PlotType> PlotTypeList { get; } =
            Enum.GetValues(typeof(PlotType))
            .Cast<PlotType>()
            .ToList();

        // 绘图绑定数据
        public ISeries[] Series { get; set; }

        public Axis[] XAxes { get; set; }

        public Axis[] YAxes { get; set; }

        public LabelVisual Title { get; set; } = new LabelVisual
        {
            Text = "MTExecutionVisulization",
            TextSize = 15,
            Padding = new LiveChartsCore.Drawing.Padding(15),
            Paint = new SolidColorPaint(SKColors.DarkSlateGray)
        };

        public Visibility ChartVisibility { get; set; } = Visibility.Hidden;

        public Visibility PieChartVisibility { get; set; } = Visibility.Hidden;

        private bool _isInitialSelection = true; // 初始选择标志

        //MT结果可视化服务
        private MTVisualizationService _mTVisualizationSerive;

        // datagrid的数据源
        public ObservableCollection<MetamorphicRelations_QueryResultData> Data { get; set; }

        // 进度条属性 IsIndeterminate
        public bool IsIndeterminate { get; set; }

        // 进度条属性 Visibility
        public Visibility Visibility { get; set; }

        //绘图数据缓存
        private Dictionary<(string, PlotType), (ISeries[] Series, Axis[] XAxes, Axis[] YAxes)> _visualizationDataCache = new();
        // 绘图更新标识
        private bool isUpdate = false;

        //MT执行结果Csv文件路径
        private string CsvFilePath { get; set; } = string.Empty;

        //MT报告数据路径
        public string STCPath { get; set; }

        public string FTCPath { get; set; }

        public string CombinedPath { get; set; }

        ////MT报告路径
        public string HtmlReportPath { get; set; } = string.Empty;

        public string ExcelReportPath { get; set; } = string.Empty;

        public string WrodReportPath { get; set; } = string.Empty;

        public string PdfReportPath { get; set; } = string.Empty;



        public void OnNavigatedFrom()
        {
        }

        public void OnNavigatedTo()
        {
            if (DataGridSelectedItem != null)
            {
                var metamorphicRelation = _metamorphicRelationSerive.GetMRById(DataGridSelectedItem.IdMR);
                mr = metamorphicRelation;
                string[] strarry = metamorphicRelation.ApplicationName.Split(':');
                this.InputPatternSympy = metamorphicRelation.InputPatterntosympy;
                this.OutputPatternSympy = metamorphicRelation.OutputPatterntosympy;
                this.DimensionOfInputPattern = metamorphicRelation.DimensionOfInputPattern;
                this.DimensionOfOutputPattern = metamorphicRelation.DimensionOfOutputPattern;

                reload_CodeNamecmbItemsSource();

                // 默认所选择的蜕变关系记录对应着的应用程序
                var idapplication = _applicationSerive.GetApplicationId(DataGridSelectedItem.ApplicationName);
                var application = _applicationSerive.GetApplication(idapplication);
                Application = application;
                CodeName = Application.CodeName;
                for (int i = 0; i < Applications.Count; i++)
                {
                    var app = Applications[i];
                    if (app.IdApplication == application.IdApplication)
                    {
                        SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        public void reload_CodeNamecmbItemsSource()
        {
            var applications = new ObservableCollection<MetBench_Domain.Application>();
            var codeNameOptions = new ObservableCollection<string>();

            if (DataGridSelectedItem != null)
            {
                var metamorphicRelation = _metamorphicRelationSerive.GetMRById(DataGridSelectedItem.IdMR);
                string[] strarry = metamorphicRelation.ApplicationName.Split(':');

                for (int i = 0; i < strarry.Length; i++)
                {
                    var applicationName = strarry[i];
                    var idApplication = _applicationSerive.GetApplicationId(applicationName);
                    var application = _applicationSerive.GetApplication(idApplication);
                    var codeName = application.CodeName;
                    codeNameOptions.Add(codeName);
                    applications.Add(application);
                }

                Applications = applications;
                CodeNameOptions = codeNameOptions;
            }
        }

        // 构造函数
        public LocalizedTextProvider Localization { get; }

        public MTExecutionViewModel(IPageService pageService, INavigationService navigationService, ApplicationService applicationSerive, MetamorphicRelationService metamorphicRelationSerive, MTVisualizationService mTVisualizationSerive, LocalizedTextProvider localization)
        {
            _navigationService = navigationService;
            _pageService = pageService;
            _applicationSerive = applicationSerive;
            _metamorphicRelationSerive = metamorphicRelationSerive;
            _mTVisualizationSerive = mTVisualizationSerive;
            Localization = localization;
            IsIndeterminate = false;
            Visibility = Visibility.Collapsed;


        }

        // MT参数验证器
        private ValidationResult GetValidationResult(MTParam mTParam)
        {
            var validator = new MTValidator();
            var result = validator.Validate(mTParam);
            return result;
        }

        // 创建MT参数实例
        private MTParam Create()
        {
            var mtParam = new MTParam();
            mtParam.InputPatternSympy = InputPatternSympy;
            mtParam.OutputPatternSympy = OutputPatternSympy;
            mtParam.MinParam = MinParam;
            mtParam.MaxParam = MaxParam;
            mtParam.ExecutNumber = ExecutNumber;
            mtParam.Threshold = Threshold;
            return mtParam;
        }

        // 提示信息弹窗
        public bool showMessage(string message, string title)
        {
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = title,
                Content = message,
            };
            uiMessageBox.CloseButtonAppearance = 0;
            uiMessageBox.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            uiMessageBox.CloseButtonText = "OK";
            var messageResult = uiMessageBox.ShowDialogAsync().Result.ToString();
            //primary为第一个按钮
            return messageResult == "Primary" ? true : false;
        }

        public void LoadAndRenderData(PlotType plotType)
        {

            string filePath = CsvFilePath;

            // 检查是否更新绘图数据
            if (!isUpdate)
            {
                // 检查缓存是否存在
                if (_visualizationDataCache.TryGetValue((filePath, plotType), out var cachedData))
                {
                    // 从缓存恢复数据
                    Series = cachedData.Series;
                    XAxes = cachedData.XAxes;
                    YAxes = cachedData.YAxes;
                    PlotTypeComboBoxSelectedIndex = (int)plotType;
                    SelectedType = plotType;
                    if (PlotTypeComboBoxSelectedIndex == 0)
                    {
                        PieChartVisibility = Visibility.Visible;
                        ChartVisibility = Visibility.Hidden;
                    }
                    else
                    {
                        PieChartVisibility = Visibility.Hidden;
                        ChartVisibility = Visibility.Visible;
                    }

                    return;
                }
            }

            try
            {
                ClearVisualizationCache();
                // 1. 初始化数据（仅当缓存不存在时执行）
                _mTVisualizationSerive.Initialize(filePath, plotType);

                // 2. 获取新数据
                var (series, xAxes, yAxes) = _mTVisualizationSerive.GetVisualizationData();

                // 3. 存入缓存
                foreach (PlotType type in Enum.GetValues(typeof(PlotType)))
                {
                    _visualizationDataCache[(filePath, plotType)] = (series, xAxes, yAxes);
                }
                //_visualizationDataCache[(filePath, plotType)] = (series, xAxes, yAxes);

                // 4. 更新UI
                Series = series;
                XAxes = xAxes;
                YAxes = yAxes;
                PlotTypeComboBoxSelectedIndex = (int)plotType;
                SelectedType = plotType;
                if (PlotTypeComboBoxSelectedIndex == 0)
                {
                    PieChartVisibility = Visibility.Visible;
                    ChartVisibility = Visibility.Hidden;
                }
                else
                {
                    PieChartVisibility = Visibility.Hidden;
                    ChartVisibility = Visibility.Visible;
                }

                // 将更新标识复原
                isUpdate = false;
            }
            catch (Exception ex)
            {
                showMessage($"Failed to load data: {ex.Message}", "Tips");

                // 可选：清除错误缓存
                _visualizationDataCache.Remove((filePath, plotType));
            }
        }

        // 3. 添加缓存清理方法（当数据源变化时调用）
        public void ClearVisualizationCache()
        {
            _visualizationDataCache.Clear();
        }

        // 根据路径获取图片
        public BitmapImage GetImage(string path)
        {
            var path1 = path;
            BitmapImage bitmap = null;
            // 设置图片路径
            if (System.IO.File.Exists(path))
            {
                bitmap = new BitmapImage(new Uri(path, UriKind.Absolute));
            }
            else
            {
                System.Windows.MessageBox.Show("执行失败！", "提示", System.Windows.MessageBoxButton.OK);
            }
            return bitmap;
        }

        // 二元关系执行
        public async Task btn_AutoMR2()
        {
            if (Application != null)
            {
                var selectedIndex = SelectedIndex;
                var ApplicationsIndex = selectedIndex;
                CodeName = SelectedValue;
                // 获取应用程序的Id
                var idapplication = Applications[ApplicationsIndex].IdApplication;

                //获取应用程序的Id
                //var idapplication = Application.IdApplication;

                // 创建压缩上传文件类对象
                FileCompressionAndStorageUtility ft = new FileCompressionAndStorageUtility(_applicationSerive);
                // 接收Python脚本运行输出台的结果
                string[] splitStrings = null;

                // 解压对应的应用程序SUT
                ft.ExtractZipFilesFromDatabase(idapplication);
                var mtparam = Create();
                var validationResult = GetValidationResult(mtparam);
                if (!validationResult.IsValid)
                {
                    var message = string.Empty;
                    for (int i = 0; i < validationResult.Errors.Count; i++)
                    {
                        if (i == 0)
                        {
                            message += validationResult.Errors[i].ErrorMessage;
                        }
                        else
                        {
                            message += "\n";
                            message += validationResult.Errors[i].ErrorMessage;
                        }
                    }
                    showMessage(message, "Tips");
                    return;
                }

                if (CodeName != string.Empty)
                {

                    // 设置进度条状态为可见和不确定状态
                    IsIndeterminate = true;
                    Visibility = Visibility.Visible;
                    try
                    {
                        // 使用异步的方式实现
                        AutoRunMR_Await ar = new AutoRunMR_Await();
                        TestTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string outputString = await ar.AutorunMR2Async(InputPatternSympy, OutputPatternSympy, CodeName, DimensionOfInputPattern, DimensionOfOutputPattern, MinParam, MaxParam, ExecutNumber, Threshold);
                        splitStrings = outputString.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    }
                    catch (Exception ex)
                    {
                        showMessage("MT执行失败！", "Tips");
                        return;
                    }
                    finally
                    {
                        // 结束时将进度条状态更新为不可见和确定状态
                        IsIndeterminate = false;
                        Visibility = Visibility.Collapsed;
                    }
                }

                // 检查是否成功分割为7部分
                if (splitStrings != null && splitStrings.Length == 7)
                {
                    // STC结果路径
                    string stcPath = splitStrings[0];
                    STCPath = stcPath;

                    // FTC结果路径
                    string ftcPath = splitStrings[1];
                    FTCPath = ftcPath;

                    // Combined结果路径
                    string combinedPath = splitStrings[2];
                    CombinedPath = combinedPath;

                    // 通过比率
                    Passrate = splitStrings[3];

                    // 失败比率
                    Failurerate = splitStrings[4];

                    // 图片路径
                    string imagepath = splitStrings[5];
                    Image = GetImage(imagepath);

                    // 结果csv文件
                    string csvPath = splitStrings[6];
                    if (csvPath.EndsWith(".csv"))
                    {
                        CsvFilePath = combinedPath;
                        isUpdate = true;
                        LoadAndRenderData(PlotType.Pie);
                    }
                    try
                    {
                        IsIndeterminate = true;
                        Visibility = Visibility.Visible;
                        GenerateMTReport();
                    }
                    catch (Exception ex)
                    {
                        showMessage("生成报告失败！", "Tips");
                    }
                    finally
                    {
                        // 结束时将进度条状态更新为不可见和确定状态
                        IsIndeterminate = false;
                        Visibility = Visibility.Collapsed;
                    }
                }

                else
                {
                    // 如果分割失败，执行相应的逻辑，例如弹出提示框
                    var msg = "执行失败！";
                    showMessage(msg, "Tips");
                    return;
                }
            }
            else
            {
                var msg = "请选择被测程序！";
                showMessage(msg, "Tips");
            }

            if (Data == null)
            {

                var msg = "请选择蜕变关系！";
                var res = showMessage(msg, "Tips");

                if (!res)
                {
                    // 进行页面跳转 跳转到MRManagement页面
                    var targetPageType = typeof(Views.Pages.MRManagementPage);
                    var page = _pageService.GetPage(targetPageType) as MRManagementPage;
                    _navigationService.Navigate(targetPageType);
                }
            }
        }

        // 生成MT报告
        private void GenerateMTReport()
        {
            List<string> list = new List<string>() { STCPath, FTCPath, CombinedPath };
            for (int i = 0; i < list.Count; i++)
            {
                var filepath = list[i];

                // 检查源文件是否存在
                if (!File.Exists(filepath))
                {
                    showMessage("生成报告失败！", "Tips");
                    return;
                }
            }

            string projectRoot = string.Empty;

            // 获取当前应用程序的基础目录
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // 从当前程序集位置向上查找，直到找到解决方案根目录
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                if (Directory.GetFiles(directory.FullName, "*.sln").Any())
                {
                    projectRoot = directory.FullName;
                }

                directory = directory.Parent;
            }

            if (projectRoot == null)
            {
                showMessage("生成报告失败！", "Tips");
                return;
            }

            // 构建目标目录路径
            string destinationDirectory = Path.Combine(
                projectRoot,
                "MetBench_MTReport"
                );

            string pdfPath = Path.Combine(
                destinationDirectory,
                "MTTestReport_Pdf.pdf"
                );
            PdfReportPath = pdfPath;

            string wordPath = Path.Combine(
                destinationDirectory,
                "MTTestReport_Word.docx"
                );
            WrodReportPath = wordPath;

            string htmlPath = Path.Combine(
                destinationDirectory,
                "MTTestReport_Html.html"
                );
            HtmlReportPath = htmlPath;

            string excelPath = Path.Combine(
                destinationDirectory,
                "MTTestReport_Excel.xlsx"
                );
            ExcelReportPath = excelPath;

            Dictionary<string, string> dict = new Dictionary<string, string>()
            {
                { "Pdf",  pdfPath },
                { "Word", wordPath },
                { "Html", htmlPath },
                { "Excel", excelPath }
            };

            try
            {
                MTReportGeneratorService _mtReportGeneratorSerive = new MTReportGeneratorService();
                // 加载数据
                _mtReportGeneratorSerive.LoadData(STCPath, FTCPath, CombinedPath);

                _mtReportGeneratorSerive.GenerateAllReports(pdfPath, wordPath, htmlPath, excelPath, Application.Name, mr.InputPattern, mr.OutputPattern, TestTime);
            }
            catch (Exception ex)
            {
                showMessage("生成报告失败！", "Tips");
                return;
            }
        }

        // 跳转到MTReportGenerator页面
        public void btn_MTReport()
        {
            var targetPageType = typeof(Views.Pages.MTReportGeneratorPage);
            var page = _pageService.GetPage(targetPageType) as MTReportGeneratorPage;

            var viewModel = page.ViewModel;
            viewModel._isNavigate = true;
            viewModel.PdfReportPath = PdfReportPath;
            viewModel.WrodReportPath = WrodReportPath;
            viewModel.ExcelReportPath = ExcelReportPath;
            viewModel.HtmlReportPath = HtmlReportPath;
            _navigationService.Navigate(targetPageType);
        }

        public void btn_Cancle()
        {
            MinParam = "";
            MaxParam = "";
            ExecutNumber = "";
            Threshold = "1e-4";
            // 默认第一个应用程序
            Application = Applications[0];
            CodeName = Applications[0].CodeName;
            SelectedIndex = 0;
        }

        public void com_SelectionChanged()
        {
            // 跳过初始选择
            if (_isInitialSelection)
            {
                _isInitialSelection = false;
                return;
            }

            // 确保有选中的项
            if (PlotTypeComboBoxSelectedIndex < 0 || PlotTypeComboBoxSelectedIndex >= Enum.GetValues(typeof(PlotType)).Length)
                return;

            try
            {
                PlotType selectedPlotType = (PlotType)PlotTypeComboBoxSelectedIndex;

                switch (selectedPlotType)
                {
                    case PlotType.Pie:
                        LoadAndRenderData(PlotType.Pie);
                        break;
                    case PlotType.Scatter:
                        LoadAndRenderData(PlotType.Scatter);
                        break;
                    case PlotType.Line:
                        LoadAndRenderData(PlotType.Line);
                        break;
                    default:
                        Debug.WriteLine($"未知的图表类型: {selectedPlotType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"图表切换失败: {ex.Message}");
                showMessage($"图表切换失败: {ex.Message}", "错误");
            }
        }

        public AutoRunMR_Await AutoRunMR_Await
        {
            get => default;
            set
            {
            }
        }

        public MetamorphicRelationService MetamorphicRelationService
        {
            get => default;
            set
            {
            }
        }

        public ApplicationService ApplicationService
        {
            get => default;
            set
            {
            }
        }

        public MTParam MTParam
        {
            get => default;
            set
            {
            }
        }

        public MTValidator MTValidator
        {
            get => default;
            set
            {
            }
        }
    }
}
