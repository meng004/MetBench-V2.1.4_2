using CommunityToolkit.Mvvm.ComponentModel;
using MetBench_BLL;
using CommunityToolkit.Mvvm.Input;
using MetBench_Client.Helpers;
using MetBench_Client.Services;
using MetBench_Client.Views.Pages;
using MetBench_UI.Localization;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
    public partial class MRRecommendationViewModel : ObservableObject, INavigationAware
    {
        // 导航服务 用于页面切换
        private INavigationService _navigationService;

        // 页面服务 用于获取页面实体
        private IPageService _pageService;

        // 蜕变关系管理服务
        private MetamorphicRelationService _metamorphicRelationSerive;

        // 应用程序管理服务
        private ApplicationService _applicationSerive;

        // 蜕变关系推荐服务
        private MRRecommendationService _mrRecommendationSerive;

        // DataGrid控件的数据源
        public ObservableCollection<MRRecommendationVIsualResult> Data { get; set; }

        // RecommendType_ComboBox的数据源
        public List<string> RecommendTypeList { get { return new List<string>() { "语法相似推荐", "语义相似推荐" }; } }

        // RecommendType_ComboBox的SelectedIndex
        // 0表示语法相似推荐 1语义相似推荐
        public int RecommendType_ComboBoxSelectedIndex { get; set; } = 1;

        // RecommendType_ComboBox的SelectedValue
        public string RecommendType_ComboBoxSelectedValue { get; set; } = string.Empty;

        // 显示的代码文件名的数据绑定属性
        public string CodeName { get; set; } = string.Empty;

        // 输入参数个数
        public string InputParamCount { get; set; } = string.Empty;

        // 输入参数范围
        public string InputParamRanges { get; set; } = string.Empty;

        // 输出参数类型
        public string InputDatatypes { get; set; } = string.Empty;

        // 上传文件的标志
        private bool isUpload = false;

        // 上传文件的路径
        private string programFilePath = string.Empty;



        // 每页数据行数量
        public int DataCountPerPage { get; set; } = 5;

        // 每页最大数据行数量
        public int MaxPageCount { get; set; } = 10;

        // 页码
        public int PageIndex { get; set; } = 1;

        // 中间数据集
        private ObservableCollection<MRRecommendationVIsualResult> datas = new ObservableCollection<MRRecommendationVIsualResult>();

        // 进度条属性 IsIndeterminate 
        public bool IsIndeterminate { get; set; }=false;

        // 进度条属性 Visibility
        public Visibility Visibility { get; set; }=Visibility.Hidden;    

        // 实现接口 INavigationAware
        public void OnNavigatedTo()
        {
        }

        public void OnNavigatedFrom()
        {
        }

        public LocalizedTextProvider Localization { get; }

        public MRRecommendationViewModel(MetamorphicRelationService metamorphicRelationSerive, ApplicationService applicationSerive, MRRecommendationService mRRecommendationSerive, IPageService pageService, INavigationService navigationService, LocalizedTextProvider localization)
        {
            _metamorphicRelationSerive = metamorphicRelationSerive;
            _applicationSerive = applicationSerive;
            _mrRecommendationSerive = mRRecommendationSerive;
            _pageService = pageService;
            _navigationService = navigationService;
            Localization = localization;
            //_eventAggregator = eventAggregator;
            //_eventAggregator.Subscribe(this);
            this.reload_ItemsSource();
            //_eventAggregator = eventAggregator;
        }

        // 渲染MR推荐数据
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
            Data = new ObservableCollection<MRRecommendationVIsualResult>(booksPaged);
        }

        [RelayCommand]
        private Task BackUploadAsync() => btnBack_Click();

        [RelayCommand]
        private Task AddCodeAsync() => btnAddCode_Click();

        [RelayCommand]
        private Task RecommendAsync() => btnRecommend_Click();

        [RelayCommand]
        private void CancelRecommendation() => btn_Cancle();

        [RelayCommand]
        private void EditRecommendedMr(int id) => btn_EditSelectedMR(id);

        [RelayCommand]
        private void EditRecommendedApplication(int id) => btn_EditSelectedApplication(id);

        private TargetProgram Create()
        {
            // 参数个数默认为1
            var count = 1;
            int.TryParse(InputParamCount, out count);
            var paramCount = count;
            return new FunctionProgram(ProgramLanguage.Python, programFilePath, CodeName, paramCount, InputParamRanges, InputDatatypes);
        }

        //MRDetector参数验证器
        private ValidationResult GetValidationResult(FunctionProgram functionProgram)
        {
            var validator = new MRDetectorValidator();
            var result = validator.Validate(functionProgram);
            return result;
        }

        // 提示信息弹窗
        public async Task<bool> showMessageAsync(string message, string title)
        {
            return await UiDialog.ShowMessageAsync(message, title);
        }

        // 撤回上传
        public async Task btnBack_Click()
        {
            programFilePath = string.Empty;
            CodeName = string.Empty;
            await showMessageAsync("撤销上传成功", "Tips");
        }

        // 将目标程序复制粘贴到fuction文件夹
        public async Task btnAddCode_Click()
        {
            var confirmResult = await UiDialog.ConfirmAsync("确认上传文件吗？", "Tips");
            if (!confirmResult)
            {
                return;
            }

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

                    if (!fileExtension.Equals(".py") && !fileExtension.Equals(".java"))
                    {
                        string msg = "目前仅支持Python或Java语言程序！";
                        await showMessageAsync(msg, "Tips");
                        btn_Cancle();
                        return;
                    }
                }
                programFilePath = filePath;
                await showMessageAsync("文件上传成功！", "Tips");
            }
        }

        // 执行MR识别
        public async Task btnRecommend_Click()
        {
            if (!(programFilePath.Length > 0))
            {
                await showMessageAsync("请上传目标程序", "Tips");
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
                var mrRecommendationResults = new ObservableCollection<MRRecommendationResult>();
                var candidatePrograms = _applicationSerive.GetApplications();

                // 语法相似推荐
                if (RecommendType_ComboBoxSelectedIndex==0) 
                {
                    mrRecommendationResults = await _mrRecommendationSerive.SyntaxMRRRecommend(program, candidatePrograms);
                    if (mrRecommendationResults == null)
                    {
                        await showMessageAsync("没有可推荐的MR！","Tips");
                        return;
                    }
                    var viusalResults = await convertCollection(mrRecommendationResults);
                    var list  = viusalResults.ToList();
                    datas = viusalResults;
                    reload_ItemsSource();
                    await showMessageAsync($"共推荐{list.Count}条MR", "Tips");
                }

                // 语义相似推荐
                else
                {
                    mrRecommendationResults = await _mrRecommendationSerive.SemanticMRRRecommend(program, candidatePrograms);
                    if (mrRecommendationResults == null)
                    {
                        await showMessageAsync("没有可推荐的MR！", "Tips");
                        return;
                    }
                    var viusalResults = await convertCollection(mrRecommendationResults);
                    var list = viusalResults.ToList();
                    datas = viusalResults;
                    reload_ItemsSource();
                    await showMessageAsync($"共推荐{list.Count}条MR", "Tips");
                }
            }
            finally
            {
                // 结束时将进度条状态更新为不可见和确定状态
                IsIndeterminate = false;
                Visibility = Visibility.Collapsed; 
            }
        }

        private async Task<ObservableCollection<MRRecommendationVIsualResult>> convertCollection(ObservableCollection<MRRecommendationResult> mRRecommendationResults ) 
        {
            if (mRRecommendationResults == null) 
            {
                return new ObservableCollection<MRRecommendationVIsualResult>();
            }

            var mRRecommendationResultsList = mRRecommendationResults.ToList();
            var results = new ObservableCollection<MRRecommendationVIsualResult>();
            var latextosympy = new Latextosympy_Await();
            foreach (var mRRecommendationResult in mRRecommendationResultsList) 
            {
                var targetProgram = mRRecommendationResult.TargetProgram;
                var application  = mRRecommendationResult.Application;
                var mrs = mRRecommendationResult.MetamorphicRelations;
                var similarity = mRRecommendationResult.Similarity;
                var domainNames = application.DomainName == string.Empty ? "无" : application.DomainName;
                var mrWithDecimalsList = mrs.ToList();
                foreach (var mrWithDecimal in mrWithDecimalsList)
                {
                    var mr = mrWithDecimal.Relation;
                    var value = mrWithDecimal.Value;
                    var inputPattern = mr.InputPattern;
                    var inputPatternImagePath = await latextosympy.LatextoImageAsync(inputPattern);
                    var outputPattern = mr.OutputPattern;
                    var outputPatternImagePath = await latextosympy.LatextoImageAsync(outputPattern);
                    var mRRecommendationVIsualResult = new MRRecommendationVIsualResult()
                    {
                        TargetProgram = targetProgram as FunctionProgram,
                        Application = application,
                        MetamorphicRelation = mr,
                        Value = value,
                        Similarity = similarity,
                        InputPatternImagepath = inputPatternImagePath,
                        OutputPatternImagepath = outputPatternImagePath,
                        DomainNames = domainNames
                    };
                    results.Add(mRRecommendationVIsualResult);
                }
            }

            return results;
        }

        // 重置输入
        public void btn_Cancle()
        {
            InputParamCount = string.Empty;
            InputParamRanges = string.Empty;
            InputDatatypes = string.Empty;
            programFilePath = string.Empty;
            CodeName = string.Empty;
            RecommendType_ComboBoxSelectedIndex = 1;
        }

        // 辅助方法，检验是否为正整数
        private bool IsValidPositiveInteger(string input)
        {
            return int.TryParse(input, out int result) && result > 0;
        }

        // 查看推荐的蜕变关系详细信息
        public void btn_EditSelectedMR(int id) 
        {
            var idMR = id;
            var mixResults = _metamorphicRelationSerive.showMultTwoTableResult();
            var selectedItem = mixResults.FirstOrDefault(x => x.IdMR == idMR);
            var targetPageType = typeof(Views.Pages.MRManagementPage);
            var page = _pageService.GetPage(targetPageType) as MRManagementPage;
            var viewModel = page.ViewModel;
            viewModel._isNavigate = true;
            viewModel.DataGridSelectItem = selectedItem;
            _navigationService.Navigate(targetPageType);
        }

        // 查看推荐的应用程序详细信息
        public void btn_EditSelectedApplication(int id)
        {
            var idApplication = id;
            var selectedItem = _applicationSerive.GetApplication(idApplication);
            var targetPageType = typeof(Views.Pages.ApplicationManagementPage);
            var page = _pageService.GetPage(targetPageType) as ApplicationManagementPage;
            var viewModel = page.ViewModel;
            viewModel._isNavigate = true;
            viewModel.DataGridSelectedItem = selectedItem;
            _navigationService.Navigate(targetPageType);
        }
    }
}
