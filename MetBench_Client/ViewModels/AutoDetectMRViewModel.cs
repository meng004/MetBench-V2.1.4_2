using CommunityToolkit.Mvvm.ComponentModel;
using MetBench_BLL;
using CommunityToolkit.Mvvm.Input;
using MetBench_Client.Helpers;
using MetBench_Client.Services;
using MetBench_Client.Util;
using MetBench_UI.Localization;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace MetBench_Client.ViewModels
{
    public partial class AutoDetectMRViewModel : ObservableObject, INavigationAware, IHandle<ApplicationAddEvent>, IHandle<MetamorphicRelationOperationEvent>
    {
        // 导航服务 用于页面切换
        private INavigationService _navigationService;

        // 页面服务 用于获取页面实体
        private IPageService _pageService;

        // 应用程序管理服务
        private ApplicationService _applicationSerive;

        // 蜕变关系管理服务
        private MetamorphicRelationService _metamorphicRelationSerive;

        // 事件聚合
        private IEventAggregator _eventAggregator;

        // 蜕变关系识别服务
        private MRDetector _detector;

        // 显示的代码文件名的数据绑定属性
        public string CodeName { get; set; } = string.Empty;

        // 输入参数个数
        public string InputParamCount { get; set; } = string.Empty;

        // 输入参数范围
        public string InputParamRanges { get; set; } = string.Empty;

        // 输出参数类型
        public string InputDatatypes { get; set; } = string.Empty;

        //// 绑定目标程序Code的数据绑定属性
        //public byte[] Code { get; set; }

        // datagrid的数据源
        public ObservableCollection<MRDetectorCollection> Data { get; set; }= new ObservableCollection<MRDetectorCollection>();

        // 进度条属性 IsIndeterminate 
        public bool IsIndeterminate { get; set; }

        // 进度条属性 Visibility
        public Visibility Visibility { get; set; }

        // 每页数据行数量
        public int DataCountPerPage { get; set; } = 5;

        // 每页最大数据行数量
        public int MaxPageCount { get; set; } = 10;

        // 页码
        public int PageIndex { get; set; } = 1;

        // 上传文件的标志
        private bool isUpload = false;

        // 上传文件的路径
        private string programFilePath = string.Empty;

        // 识别并解析的MRcsv文件（未转换未Latex格式）
        private string mrCsvPath = string.Empty;

        // 解析结果
        private ParsedResult ParsedResult { get; set; }

        // 中间数据集
        private ObservableCollection<MRDetectorCollection> datas = new ObservableCollection<MRDetectorCollection>();

        //// 验证识别数据重复标志
        //private bool isStore = false;

        public void OnNavigatedFrom()
        {
        }

        public void OnNavigatedTo()
        {
        }

        public void reload_ItemsSource()
        {
            Data = datas;
            // 展示识别到的MR关系并进行分页
            var index = PageIndex;

            // 按关键字查询
            var mrs = Data.ToList();

            // 分页数量
            var count = (double)(mrs.Count * 1.0 / DataCountPerPage);

            //// 正常使用需要将其解除注释
            MaxPageCount = (int)Math.Ceiling(count);

            // 按页码及每页记录数
            var booksPaged = mrs.Skip((index - 1) * DataCountPerPage).Take(DataCountPerPage).ToList();
            var paged = mrs.Skip((index - 1) * DataCountPerPage).Take(DataCountPerPage).ToList();
            Data = new ObservableCollection<MRDetectorCollection>(booksPaged);
        }

        [RelayCommand]
        private Task BackUploadAsync() => btnBack_Click();

        [RelayCommand]
        private Task AddCodeAsync() => btnAddCode_Click();

        [RelayCommand]
        private void CancelDetection() => btn_Cancle();

        [RelayCommand]
        private Task AutoDetectAsync() => btn_AutoDetectMR();

        [RelayCommand]
        private Task StoreMrAsync() => btn_StoreMR();

        [RelayCommand]
        private Task ExportCsvAsync() => btn_ExportCsv();

        // 构造函数订阅事件
        // 构造函数
        // 本地化文本提供者
        public LocalizedTextProvider Localization { get; }

        public AutoDetectMRViewModel(IPageService pageService , INavigationService navigationService,ApplicationService applicationSerive,MetamorphicRelationService metamorphicRelationSerive , MRDetector mRDetector, IEventAggregator eventAggregator, LocalizedTextProvider localization)
        {
            _navigationService = navigationService;
            _pageService = pageService;
            _applicationSerive = applicationSerive;
            _metamorphicRelationSerive = metamorphicRelationSerive;
            this._eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
            _detector = mRDetector;
            IsIndeterminate = false;
            Visibility = Visibility.Collapsed;
            Localization = localization;
        }

        //MRDetector参数验证器
        private ValidationResult GetValidationResult(FunctionProgram functionProgram)
        {
            var validator = new MRDetectorValidator();
            var result = validator.Validate(functionProgram);
            return result;
        }

        private TargetProgram Create()
        {
            // 参数个数默认为1
            var count = 1;
            int.TryParse(InputParamCount, out count);
            var paramCount = count;
            return new FunctionProgram(ProgramLanguage.Python, programFilePath, CodeName, paramCount, InputParamRanges,InputDatatypes);
        }

        // 提示信息弹窗
        public async Task<bool> showMessageAsync(string message, string title)
        {
            return await UiDialog.ShowMessageAsync(message, title);
        }

        // 将目标程序复制粘贴到fuction文件夹
        public async Task btnAddCode_Click()
        {
            var confirmResult = await UiDialog.ConfirmAsync("确认上传文件吗？", "Tips");
            if (!confirmResult)
            {
                return;
            }

            string recommand = "注意：目标程序需要定义一个main()函数作为程序执行的入口点！";
            await showMessageAsync(recommand, "Tips");

            OpenFileDialog openFileDialog = new OpenFileDialog();

            // 设置对话框的标题
            openFileDialog.Title = "选择要上传的文件";

            // 设置对话框的初始目录（可选）
            openFileDialog.InitialDirectory = "C:\\";

            // 设置对话框允许选择的文件类型（可选）
            openFileDialog.Filter = "所有文件 (*.*)|*.*";

            // 打开对话框
            bool? result = openFileDialog.ShowDialog();

            // 检查用户是否选择了文件
            if (result == true)
            {
                // 获取用户选择的文件路径
                string filePath = openFileDialog.FileName;

                // 获取文件名
                string CodeFileName = Path.GetFileName(filePath);

                // 目标程序文件名 如sin.py
                CodeName = CodeFileName;

                if (CodeName != null)
                {
                    string fileExtension = Path.GetExtension(CodeName);

                    if (!fileExtension.Equals(".py"))
                    {
                        string msg = "目前仅支持Python语言程序！";
                        await showMessageAsync(msg, "Tips");
                        btn_Cancle();
                        return;
                    }
                }

                // 简单验证目标程序代码是否包含main函数
                var hasMain = HasMainFunction(filePath);
                if (!hasMain)
                {
                    string msg = "目标程序需要定义一个main()函数作为程序执行的入口点！";
                    await showMessageAsync(msg, "Tips");
                    btn_Cancle();
                    return;
                }

                programFilePath = filePath;
                await showMessageAsync("文件上传成功！", "Tips");
            }
        }

        // 撤回上传
        public async Task btnBack_Click()
        {
            programFilePath = string.Empty;
            CodeName = string.Empty;
            await showMessageAsync("撤销上传成功", "Tips");
        }

        // 简单判断Python函数脚本是否实现了main函数
        private bool HasMainFunction(string pythonFilePath)
        {
            if (!File.Exists(pythonFilePath))
            {
                _ = showMessageAsync("Python文件不存在", "Tips");
                return false;
            }

            foreach (string line in File.ReadLines(pythonFilePath))
            {
                string trimmed = line.TrimStart();

                // 跳过空行和注释
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                {
                    continue;
                }

                // 检查是否包含"def main("
                if (trimmed.StartsWith("def main(", StringComparison.Ordinal)) 
                {
                      return true;
                }
            }

            return false;
        }

        // 存储识别到的MR
        public async Task btn_StoreMR()
        {
            var confirmResult = await UiDialog.ConfirmAsync("确认存储MR数据吗？", "Tips");
            if (!confirmResult)
            {
                return;
            }

            if (ParsedResult != null)
            {
                var application = ParsedResult.Application;
                var mrs = ParsedResult.MetamorphicRelations;
                try
                {
                    if (application != null)
                    {
                        var isstore = _applicationSerive.AddService(application);
                        if (isstore == 2)
                        {
                            ApplicationAddEvent applicationAddEvent = new ApplicationAddEvent() { Name = application.Name };
                            // 发布事件，刷新ApplicationManagement页面的应用程序列表
                            _eventAggregator.Publish(applicationAddEvent);
                        }
                    }

                    if (mrs != null)
                    {
                        var mrslist = mrs.ToList();
                        var count = 0;
                        foreach (var mr in mrslist)
                        {
                           var isstore = await _metamorphicRelationSerive.AddService(mr);
                           if (isstore == 2)
                           {
                                count++;
                           }
                        }

                        if (count > 0)
                        {
                            var ms = "蜕变关系添加成功";
                            MetamorphicRelationOperationEvent metamorphicRelationOperationEvent = new MetamorphicRelationOperationEvent() { message = ms };
                            // 发布事件，刷新MRDisplay页面与MetamorphicRelationManagement页面的蜕变关系列表
                            _eventAggregator.Publish(metamorphicRelationOperationEvent);
                        }
                    }

                    await showMessageAsync("存储MR数据成功！","Tips");
                }
                catch
                {
                    await showMessageAsync("存储MR数据失败！", "Tips");
                }
            }
            else
            {
                await showMessageAsync("存储MR数据失败，不存在识别到的MR数据！", "Tips");
            }
        }

        public async Task btn_ExportCsv()
        {
            var confirmResult = await UiDialog.ConfirmAsync("确认导出MR数据文件吗？", "Tips");
            if (!confirmResult)
            {
                return;
            }

            if (mrCsvPath != string.Empty)
            {
                var targetDirectory = string.Empty;
             
                var folderDialog = new OpenFolderDialog()
                {
                    Title = "选择文件夹",
                    InitialDirectory = @"C:\"
                };
                var result = folderDialog.ShowDialog();
                // 检查用户是否选择了文件
                if (result == true)
                {
                    // 获取用户选择的文件路径
                    string filePath = folderDialog.FolderName;
                    targetDirectory = filePath;
                }
               

                var isExport = _detector.ExportMRResults(targetDirectory, mrCsvPath);
                if (isExport)
                {
                    await showMessageAsync("MR数据文件导出成功！", "Tips");
                }
                else
                {
                    await showMessageAsync("MR数据文件导出失败！", "Tips");
                }
            }
            else
            {
                await showMessageAsync("不存在识别的MR数据文件！", "Tips");
            }
        }

        // 执行MR识别
        public async Task btn_AutoDetectMR()
        {
            if (!(programFilePath.Length>0))
            {
                await showMessageAsync("请上传目标程序","Tips");
                btn_Cancle();
                return;
            }

            var program = Create() as FunctionProgram;
            var validationResult = GetValidationResult(program);
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
                await showMessageAsync(message, "Tips");
                return;
            }

            IsIndeterminate = true;
            Visibility = Visibility.Visible;
            try
            {

                var result = await _detector.Recognize(program);
                if (result == null)
                {
                    await showMessageAsync("识别到MR失败", "Tips");
                    return;
                }

                if (result.MetamorphicRelations != null)
                {
                    await showMessageAsync($"识别并解析了{result.MetamorphicRelations.Count}条MR数据", "Tips");
                    var resultParser = _detector.GetResultParser();
                    ParsedResult = result;
                    mrCsvPath = _detector.GetMRCsvPath();
                    datas = resultParser.GetMRDetectorCollections();
                    reload_ItemsSource();
                    return;
                }
            }
            finally 
            {
                // 结束时将进度条状态更新为不可见和确定状态
                IsIndeterminate = false;
                Visibility = Visibility.Collapsed;
            }
        }

        // 重置输入
        public void btn_Cancle()
        {
            InputParamCount = string.Empty;
            InputParamRanges = string.Empty;
            InputDatatypes = string.Empty;
            programFilePath = string.Empty;
            CodeName = string.Empty;
        }

        // 事件处理器（拆信并处理）
        public void Handle(ApplicationAddEvent message)
        {
            Console.WriteLine("正在处理事件消息！");
        }

        // 事件处理器（拆信并处理）
        public void Handle(MetamorphicRelationOperationEvent message)
        {
            Console.WriteLine("正在处理事件消息！");
        }

        public MRDetector Detector
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

        public MetamorphicRelationService MetamorphicRelationService
        {
            get => default;
            set
            {
            }
        }
    }
}
