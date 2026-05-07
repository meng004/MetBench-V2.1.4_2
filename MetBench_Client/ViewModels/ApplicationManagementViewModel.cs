using CommunityToolkit.Mvvm.ComponentModel;
using MetBench_BLL;
using MetBench_Client.Helpers;
using MetBench_Client.Models;
using MetBench_Client.Util;
using MetBench_Client.Views.Pages;
using MetBench_Client.Views.Windows;
using MetBench_Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Stylet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
//using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace MetBench_Client.ViewModels
{
    public class ApplicationManagementViewModel : ObservableObject, INavigationAware, IHandle<DomainAddEvent>, IHandle<DomainModifyEvent>, IHandle<DomainDeleteEvent>
    {
        //导航服务 用于页面切换
        private INavigationService _navigationService;

        //页面服务 用于获取页面实体
        private IPageService _pageService;

        // 蜕变关系管理服务
        private MetamorphicRelationSerive _metamorphicRelationSerive;

        // 应用程序管理服务
        private ApplicationSerive _applicationSerive;

        // 应用领域管理服务
        private DomainSerive _domainSerive;

        // txbIdApplication 的数据绑定属性
        public string IdApplication { get; set; } = "0";

        // txbName 的数据绑定属性
        public string Name { get; set; } = string.Empty;

        // txbDescription 的数据绑定属性
        public string Description { get; set; } = string.Empty;

        // txbProgrammingLanguage 的数据绑定属性
        public string ProgrammingLanguage { get; set; } = string.Empty;

        // txbLineOfCode 的数据绑定属性
        public string LineOfCode { get; set; } = string.Empty;

        //上传的压缩文件名的数据绑定属性
        public string Codename { get; set; } = string.Empty;

        //显示的代码文件名的数据绑定属性
        public string CodeName { get; set; } = string.Empty;

        //测试用例程序的数据绑定属性
        public byte[] SourceTestCase { get; set; }

        //测试用例名(数据库)的数据绑定属性
        public string SourceTestCaseName { get; set; } = string.Empty;

        //测试用例名(前端显示)的数据绑定属性
        public string SourceTestCasename { get; set; } = string.Empty;

        // txbDOI的数据绑定属性
        public string DOI { get; set; } = string.Empty;

        // txbUrl的数据绑定属性
        public string Url { get; set; } = string.Empty;

        //绑定Code的数据绑定属性
        public byte[] Code { get; set; }

        // 绑定ApplicationParams窗口的DataGrid的参数集合 
        public ObservableCollection<ApplicationParameter> Params { get; set; } = new ObservableCollection<ApplicationParameter>();

        // 输入参数集合
        public ObservableCollection<ApplicationParameter> InputParameters { get; set; } = new ObservableCollection<ApplicationParameter>();

        // 输出参数集合
        public ObservableCollection<ApplicationParameter> OutputParameters { get; set; } = new ObservableCollection<ApplicationParameter>();

        // 参数类型标记 0为输入 1为输出 默认为输入
        private int ParamIndex { get; set; } = 0;

        // 参数窗口
        private INavigationWindow _navigationWindow;

        // DataGrid控件的数据源
        public ObservableCollection<Application> Data { get; set; }

        // DataGrid的SelectedItem
        public Application DataGridSelectedItem { get; set; }
        /// <summary>
        /// 事件聚合
        /// </summary>
        private IEventAggregator _eventAggregator;

        // 每页数据行数量
        public int DataCountPerPage { get; set; } = 5;

        // 每页最大数据行数量
        public int MaxPageCount { get; set; } = 10;

        // 页码
        public int PageIndex { get; set; } = 1;

        // 查询关键字
        public string ApplicationNameBoxText { get; set; } = string.Empty;


        // 接受页面传参标志
        public bool _isNavigate = false;

        private readonly IServiceProvider _serviceProvider;// 用于访问依赖注入的服务提供程序
        // 实现接口 INavigationAware
        public void OnNavigatedTo()
        {
            if (!_isNavigate)
            {
                return;
            }

            if (DataGridSelectedItem != null)
            {
                // 获取页面跳转所传递的参数 并将相关数据展示到控件中
                var selectedItem = DataGridSelectedItem;
                show();
                reload_ItemsSource();
                _isNavigate = false;
            }
        }

        public void OnNavigatedFrom()
        {

        }

        // 初始化Domain的ComboBox的数据源
        private void comboBoxDataSourceInit()
        {
            var domains = _domainSerive.GetAllDomians();
            for (int i = 0; i < domains.Count; i++)
            {
                var domain = domains[i];
                ;
                DomainExs.Add(new DomainEx(domain));
            }
            var last = new DomainEx(new Domain() { Name = "Other" });
            DomainExs.Add(last);
        }

        // 构造函数
        public ApplicationManagementViewModel(MetamorphicRelationSerive metamorphicRelationSerive, ApplicationSerive applicationSerive, DomainSerive domainSerive, IPageService pageService, INavigationService navigationService, IEventAggregator eventAggregator, IServiceProvider serviceProvider)
        {
            this._metamorphicRelationSerive = metamorphicRelationSerive;
            this._applicationSerive = applicationSerive;
            this._domainSerive = domainSerive;
            this._navigationService = navigationService;
            this._pageService = pageService;
            this._serviceProvider = serviceProvider;
            this._eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
            comboBoxDataSourceInit();
            reload_ItemsSource();
        }

        public void reload_ItemsSource()
        {
            // 获取全部的应用程序的记录
            var datas = _applicationSerive.GetApplications();

            for (int i = 0; i < datas.Count; i++)
            {
                var domainname = datas[i].DomainName;
                if (domainname == null || domainname == string.Empty)
                {
                    datas[i].DomainName = "无";
                }

                // 多个DomainName进行换行处理 \n
                else
                {
                    string[] strarray = domainname.Split(":");
                    if (strarray.Length > 1)
                    {
                        for (int j = 0; j < strarray.Length; j++)
                        {
                            if (j == 0)
                            {
                                datas[i].DomainName += strarray[j];
                            }
                            else
                            {
                                datas[i].DomainName += "\n";
                                datas[i].DomainName += strarray[j];
                            }
                        }
                    }
                }
            }
            var index = PageIndex;
            var mrs = datas.ToList();
            if (ApplicationNameBoxText.Length > 0)
            {
                // 按关键字查询
                mrs = datas.Where(x => x.Name.Contains(ApplicationNameBoxText)).ToList();

            }
            // 按关键字查询
            //var mrs = datas.Where(x => x.Name.Contains(ApplicationNameBoxText)).ToList();
            // 分页数量
            var count = (double)(mrs.Count * 1.0 / DataCountPerPage);
            //// 正常使用需要将其解除注释
            MaxPageCount = (int)Math.Ceiling(count);

            //MaxPageCount = 28;

            // 按页码及每页记录数
            var booksPaged = mrs.Skip((index - 1) * DataCountPerPage).Take(DataCountPerPage).ToList();
            var paged = mrs.Skip((index - 1) * DataCountPerPage).Take(DataCountPerPage).ToList();
            Data = new ObservableCollection<Application>(booksPaged);

            //// 查完后将搜索框的内容清空
            //ApplicationNameBoxText = string.Empty;
            //Data = datas;
        }

        // 创建应用程序实体
        private Application Create()
        {
            // 数据类型转换 将string类型转换为int类型
            var idapplication = 0;
            int.TryParse(IdApplication, out idapplication);

            var lineOfCode = 0;
            int.TryParse(LineOfCode, out lineOfCode);

            //var code = Encoding.UTF8.GetBytes(Code);
            //var sourceTestCase = Encoding.UTF8.GetBytes(SourceTestCase);

            var application = new Application();
            application.IdApplication = idapplication;
            application.Name = Name;
            application.Description = Description;
            application.ProgrammingLanguage = ProgrammingLanguage;
            application.LinesOfCode = lineOfCode;

            var application1 = _applicationSerive.GetApplication(application.IdApplication);
            application.Code = Code;
            application.CodeName = Codename;
            application.SourceTestCase = SourceTestCase;
            application.SourceTestCaseName = SourceTestCasename;
            application.DOI = DOI;
            application.Url = Url;
            application.DomainName = SelectedText;
            application.InputParameters = InputParameters.ToList() ;
            application.OutputParameters = OutputParameters.ToList();
            return application;
        }

        //Application验证器
        private ValidationResult GetValidationResult(Application application)
        {
            var validator = new ApplicationValidator();
            var result = validator.Validate(application);
            return result;
        }

        // 查重判断
        //public bool CheckApplicationExistence(Application application)
        //{

        //    var validator = new ApplicationValidator(_applicationSerive);
        //    var Existence = validator.IsDuplicate(application);
        //    if (Existence)
        //    {
        //        // 应用程序已存在
        //        return true;
        //    }

        //    // 应用程序不存在
        //    return false;
        //}

        // 重置文本框等控件内容
        public void btnCancel_Click()
        {
            IdApplication = 0.ToString();
            Name = string.Empty;
            Description = string.Empty;
            ProgrammingLanguage = string.Empty;
            LineOfCode = string.Empty;
            Codename = string.Empty;
            CodeName = string.Empty;
            SourceTestCase = null;
            SourceTestCaseName = string.Empty;
            SourceTestCasename = string.Empty;
            DataGridSelectedItem = null;
            DOI = string.Empty;
            Url = string.Empty;
            Code = null;
            resetcmbDomainnName();
            reload_ItemsSource();
        }

        //重置ApplicationNames选中的选项
        public void resetcmbDomainnName()
        {
            for (int i = 0; i < DomainExs.Count(); i++)
            {
                var domainEx = DomainExs[i];
                var isCheck = domainEx.IsChecked;
                if (isCheck == true)
                {
                    DomainExs[i].IsChecked = false;
                }
            }
            SelectedText = string.Empty;
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
            uiMessageBox.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            uiMessageBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            uiMessageBox.CloseButtonText = "OK";
            var messageResult = uiMessageBox.ShowDialogAsync().Result.ToString();
            //primary为第一个按钮
            return messageResult == "Primary" ? true : false;
        }

        //增加
        public void btnAdd_Click()
        {
            var application = Create();
            var validationResult = GetValidationResult(application);
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

            var result = _applicationSerive.AddService(application);
            if (result == 0)
            {
                var message = "添加记录 失败！";
                showMessage(message, "Tips");
            }
            else if (result == 1)
            {
                var message = "该应用程序已存在！";
                showMessage(message, "Tips");
            }
            else
            {
                var applicationName = application.Name;
                ApplicationAddEvent applicationAddEvent = new ApplicationAddEvent() { Name = applicationName };
                _eventAggregator.Publish(applicationAddEvent);
                var message = "添加记录 成功！";
                showMessage(message, "Tips");
            }
            //if (result)
            //{
            //    var applicationName = application.Name;
            //    ApplicationAddEvent applicationAddEvent = new ApplicationAddEvent() { Name = applicationName };
            //    _eventAggregator.Publish(applicationAddEvent);
            //    var message = "添加记录 成功";
            //    showMessage(message, "Tips");
            //    //System.Windows.MessageBox.Show("添加记录 成功", "提示", System.Windows.MessageBoxButton.OK);
            //}
            //else
            //{
            //    var message = "添加记录 失败";
            //    showMessage(message, "Tips");
            //    //System.Windows.MessageBox.Show("添加记录 失败", "提示", System.Windows.MessageBoxButton.OK);
            //}
            reload_ItemsSource();
            btnCancel_Click();
        }

        // 修改
        public void btnModify_Click()
        {
            if (DataGridSelectedItem == null)
            {
                var msg = "请选择要修改的应用程序！";
                showMessage(msg, "Tips");
                return;
            }

            var application = Create();
            var validationResult = GetValidationResult(application);
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
                //System.Windows.MessageBox.Show(message, "提示", System.Windows.MessageBoxButton.OK);
                return;

            }

            var applictionName = _applicationSerive.GetApplication(application.IdApplication) != null ? _applicationSerive.GetApplication(application.IdApplication).Name : string.Empty;

            var newapplicationName = application.Name;

            //if (_applicationSerive.GetApplication(application.IdApplication) == null) 
            //{
            //    var iddomain = _applicationSerive.GetApplicationId(application.Name);
            //    if (iddomain > 0)
            //    {
            //        var msg1 = "填写的Name已存在";
            //        showMessage(msg1, "Tips");
            //        return;
            //    }
            //}
            //else
            //{
            //    //将旧的Name赋值给applicationName 
            //    applictionName = _applicationSerive.GetApplication(application.IdApplication).Name;
            //    if (applictionName != newapplicationName)
            //    {
            //        //若更改前后一致，无须加入任何逻辑

            //        //应用程序名称进行修改，判断是否存在
            //        var isExist = _applicationSerive.GetApplicationId(newapplicationName) > 0;
            //        var id = _applicationSerive.GetApplicationId(newapplicationName);
            //        if (isExist)
            //        {
            //            var msg1 = "填写的Name已存在";
            //            showMessage(msg1, "Tips");
            //            return;
            //        }
            //    }
            //}

            var msg2 = "是否修改该记录?";
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Tips",
                Content = msg2,
            };
            uiMessageBox.CloseButtonAppearance = 0;
            uiMessageBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            uiMessageBox.CloseButtonText = "No";
            uiMessageBox.IsPrimaryButtonEnabled = true;
            uiMessageBox.PrimaryButtonText = "Yes";
            var messageResult = uiMessageBox.ShowDialogAsync().Result.ToString();
            var result = messageResult == "Primary" ? true : false;
            if (result)
            {
                var res = _applicationSerive.UpdateService(application);
                if (res == 0)
                {
                    var message = "修改记录 失败！";
                    showMessage(message, "Tips");
                }
                else if (res == 1)
                {
                    var message = "该应用程序已存在！";
                    showMessage(message, "Tips");
                }
                else
                {
                    ApplicationMoidfyEvent applicationMoidfyEvent = new ApplicationMoidfyEvent() { Name = applictionName, newName = newapplicationName };
                    _eventAggregator.Publish(applicationMoidfyEvent);
                    var message = "修改记录 成功！";
                    showMessage(message, "Tips");
                }
                //if (res)
                //{
                //    ApplicationMoidfyEvent applicationMoidfyEvent = new ApplicationMoidfyEvent() { Name = applictionName, newName = newapplicationName };
                //    _eventAggregator.Publish(applicationMoidfyEvent);
                //    var msg3 = "修改记录 成功";
                //    showMessage(msg3, "Tips");
                //    //System.Windows.MessageBox.Show("修改记录 成功", "提示", System.Windows.MessageBoxButton.OK);
                //}
                //else
                //{
                //    var msg3 = "修改记录 失败";
                //    showMessage(msg3, "Tips");
                //    //System.Windows.MessageBox.Show("修改记录 失败", "提示", System.Windows.MessageBoxButton.OK);
                //}
            }

            reload_ItemsSource();
            btnCancel_Click();
        }

        //删除
        public void btnDelect_Click()
        {
            if (DataGridSelectedItem == null)
            {
                showMessage("请选择要删除的应用程序！", "Tips");
                return;
            }

            var msg2 = "是否修改该记录?";
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Tips",
                Content = msg2,
            };
            uiMessageBox.CloseButtonAppearance = 0;
            uiMessageBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            uiMessageBox.CloseButtonText = "No";
            uiMessageBox.IsPrimaryButtonEnabled = true;
            uiMessageBox.PrimaryButtonText = "Yes";
            var messageResult = uiMessageBox.ShowDialogAsync().Result.ToString();
            var result = messageResult == "Primary" ? true : false;
            if (result)
            {
                var application = Create();
                var res = _applicationSerive.DeleteService(application);
                if (res)
                {
                    var name = application.Name;
                    ApplicationDeleteEvent applicationDeleteEvent = new ApplicationDeleteEvent() { Name = name };
                    _eventAggregator.Publish(applicationDeleteEvent);
                    var msg3 = "删除记录 成功";
                    showMessage(msg3, "Tips");
                }
                else
                {
                    var msg3 = "删除记录 失败";
                    showMessage(msg3, "Tips");
                }
            }

            reload_ItemsSource();
            btnCancel_Click();
        }

        //当返回的为true 选择的是每一行的应用程序数据
        public bool show()
        {

            if (DataGridSelectedItem != null)
            {
                IdApplication = DataGridSelectedItem.IdApplication.ToString();
                Name = DataGridSelectedItem.Name;
                Description = DataGridSelectedItem.Description;
                ProgrammingLanguage = DataGridSelectedItem.ProgrammingLanguage;
                LineOfCode = DataGridSelectedItem.LinesOfCode.ToString();
                if (DataGridSelectedItem.CodeName != null)
                {
                    Codename = DataGridSelectedItem.CodeName.ToString();
                }
                if (DataGridSelectedItem.Code != null)
                {
                    Code = DataGridSelectedItem.Code;
                }

                DOI = DataGridSelectedItem.DOI;
                Url = DataGridSelectedItem.Url;
                var domainname = string.Empty;
                if (DataGridSelectedItem.DomainName != "无" && DataGridSelectedItem.DomainName != null && DataGridSelectedItem.DomainName != string.Empty)
                {
                    domainname = DataGridSelectedItem.DomainName;
                }

                string[] strarray = domainname.Split(':');

                // 将之前选中的选项进行取消
                foreach (var doaminEx in DomainExs)
                {
                    doaminEx.IsChecked = false;
                }

                // Application的ComboBox进行选中
                foreach (var doaminEx in DomainExs)
                {
                    for (int j = 0; j < strarray.Length; j++)
                    {
                        var name = strarray[j];
                        var domain = doaminEx.Domain;
                        if (name == domain.Name)
                        {
                            doaminEx.IsChecked = true;
                        }
                    }
                }

                return true;
            }
            else
            {
                showMessage("请点击表格中的行数据！", "Tips");
                return false;
            }
        }

        // 解压文件并导出至系统缓存目录的metlib文件夹下
        public void btnExtractCode_Click()
        {
            FileCompressionAndStorageUtility fileCompressionAndStorageUtility = new FileCompressionAndStorageUtility(_applicationSerive);
            var application = Create();

            if (DataGridSelectedItem == null)
            {
                var message = "请选择应用程序！";
                showMessage(message, "Tips");
                return;
            }
            fileCompressionAndStorageUtility.ExtractAllZipFilesFromDatabase(application);
        }

        // 压缩文件并上传至数据库
        public void btnAddCode_Click()
        {

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
                //CopyFileToDestination(filePath);

                // 生成 ZipCode获取文件名
                string CodeFileName = Path.GetFileName(filePath);

                FileCompressionAndStorageUtility fileCompressionAndStorageUtility = new FileCompressionAndStorageUtility(_applicationSerive);
                //压缩文件并上传至数据库
                fileCompressionAndStorageUtility.ProcessFileData(filePath, CodeFileName);
                Code = fileCompressionAndStorageUtility.GetFileData();
                Codename = CodeFileName;
                if (Codename != null)
                {
                    string fileExtension = Path.GetExtension(Codename);

                    if (fileExtension.Equals(".py"))
                    {
                        ProgrammingLanguage = "Python";
                    }
                    else if (fileExtension.Equals(".java"))
                    {
                        ProgrammingLanguage = "Java";
                    }
                    else if (fileExtension.Equals(".c"))
                    {
                        ProgrammingLanguage = "C";
                    }
                }
            }
        }

        public void btnAddSourceTestCase_Click()
        {
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

                // 生成 ZipCode获取文件名
                string CodeFileName = Path.GetFileName(filePath);

                FileCompressionAndStorageUtility fileCompressionAndStorageUtility = new FileCompressionAndStorageUtility(_applicationSerive);
                //压缩文件
                fileCompressionAndStorageUtility.ProcessFileData(filePath, CodeFileName);
                SourceTestCase = fileCompressionAndStorageUtility.GetFileData();
                SourceTestCasename = CodeFileName;
            }
        }

        // 撤回上传SourceTestCaseBack
        public void btnSourceTestCaseBack_Click()
        {
            if (SourceTestCase != null)
            {
                SourceTestCase = null;
                SourceTestCasename = string.Empty;
                showMessage("撤销上传成功", "Tips");
            }
        }

        // 撤回上传
        public void btnBack_Click()
        {
            if (Code != null)
            {
                Code = null;
                Codename = string.Empty;
                ProgrammingLanguage = string.Empty;
                showMessage("撤销上传成功", "Tips");
            }
        }

        // 输入参数按钮
        public void btnInputParams_Click()
        {
            // 标记当前为输入窗口
            ParamIndex = 0;

            // 显示到窗口的DataGrid上
            Params = InputParameters;
            _navigationWindow = (_serviceProvider.GetService(typeof(ApplicationProgramsWindow)) as INavigationWindow)!;
            _navigationWindow.ShowWindow();
        }

        public void btnOutputParams_Click()
        {
            // 标记当前为输出窗口
            ParamIndex = 1;

            // 绑定数据到窗口的DataGrid上
            Params = OutputParameters;
            _navigationWindow = (_serviceProvider.GetService(typeof(ApplicationProgramsWindow)) as INavigationWindow)!;
            _navigationWindow.ShowWindow();
        }

        // 参数窗口的OK点击事件
        public void WindowbtnOK_Click()
        {
            if (ParamIndex == 0)
            {
                InputParameters = Params;
            }
            else
            {
                OutputParameters = Params;
            }
            Params.Clear();
            _navigationWindow.CloseWindow();
        }

        public void WindowbtnCancel_Click()
        {
            Params.Clear();
            _navigationWindow.CloseWindow();
        }

        // 进行页面跳转
        public void btnDomain()
        {
            var targetPageType = typeof(Views.Pages.DomainManagementPage);
            var page = _pageService.GetPage(targetPageType) as DomainManagementPage;
            _navigationService.Navigate(targetPageType);
        }

        // -----------------------------------------------
        // 以下是用于Domain的ComBox实现多选的交互逻辑

        // 私有成员变量 _selectedText
        private string _selectedText = string.Empty;

        // 公共属性 SelectedText
        public string SelectedText
        {
            get
            {
                // 这个方法用于获取当前 _selectedText 的值。当其他代码或视图需要访问选定文本时，会调用这个方法。
                // MultiSelectComboBox.BookEx
                if (_selectedText != "MetBench_Client.Models.DomainEx")
                {
                    return _selectedText;
                }
                else
                {
                    return string.Empty;
                }
            }

            set
            {
                // 此方法用于设置新的选择文本。
                // 首先，它会检查传入的值（value）是否与当前 _selectedText 相同，避免不必要的更新。
                // 如果新值与当前值不同，则将 _selectedText 更新为新值。
                // 更新 _selectedText 的同时，会调用 RaisePropertyChanged("SelectedText") 方法，通知 UI 该属性的值已发生变化。
                if (_selectedText != value)
                {
                    _selectedText = value;

                    OnPropertyChanged("SelectedText");
                }
            }
        }

        // 私有成员变量
        private ObservableCollection<DomainEx> _domains;

        // 公共属性 用于公开_applications集合
        public ObservableCollection<DomainEx> DomainExs
        {

            get
            {
                // 在get方法中，第一次访问 BookExs 时，如果 _books 还没有被初始化（值为 null）
                // ，则创建一个新的 ObservableCollection<BookEx> 实例。
                if (_domains == null)
                {
                    _domains = new ObservableCollection<DomainEx>();

                    // 集合变化事件处理
                    // 这一段代码为 _books 集合的变化（如添加或删除 BookEx 实例）注册了一个事件处理程序。
                    // CollectionChanged 事件在集合中的元素添加或移除时被触发。
                    _domains.CollectionChanged += (sender, e) =>
                    {
                        if (e.OldItems != null)
                        {
                            // 在事件处理程序中，首先检查 OldItems，如果有（即有书籍被移除），
                            // 则从每个移除的 BookEx 实例中注销 ItemPropertyChanged 方法。
                            foreach (DomainEx domainEx in e.OldItems)
                            {
                                domainEx.PropertyChanged -= ItemPropertyChanged;
                            }
                        }

                        // 然后检查 NewItems，如果有（即有新书籍被添加），
                        // 则为每个新增的 BookEx 实例注册 ItemPropertyChanged 方法。
                        if (e.NewItems != null)
                        {
                            foreach (DomainEx domainEx in e.NewItems)
                            {
                                domainEx.PropertyChanged += ItemPropertyChanged;
                            }
                        }
                    };
                }

                return _domains;
            }
        }

        // 这里略去的 ItemPropertyChanged 方法是在 BookEx 的 IsChecked 属性发生更改时被调用的。它负责更新 SelectedText。
        // 注册和注销事件处理程序确保了只有当前集合中的书籍实例的属性更改事件被处理。这样做可以避免潜在的内存泄漏和错误。
        // ----用于处理 BookEx 对象的 IsChecked 属性变化事件---。
        private void ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 检查属性名称
            if (e.PropertyName == "IsChecked")
            {
                // 获取书籍对象
                DomainEx domainEx = sender as DomainEx;

                if (domainEx != null)
                {
                    if (domainEx.Domain.Name == "Other" && domainEx.IsChecked == true)
                    {
                        //进行页面跳转 跳转至DomainManagementPage
                        var targetPageType = typeof(Views.Pages.DomainManagementPage);
                        var page = _pageService.GetPage(targetPageType) as DomainManagementPage;
                        _navigationService.Navigate(targetPageType);
                    }

                    else
                    {
                        // 收集已选中的书籍
                        IEnumerable<DomainEx> domainExs = DomainExs.Where(b => b.IsChecked == true);

                        // 构建选中的书籍名称字符串 StringBuilder 用于高效拼接字符串
                        StringBuilder builder = new StringBuilder();
                        foreach (DomainEx item in domainExs)
                        {
                            builder.Append(item.Domain.Name);
                            builder.Append(":");
                        }
                        if (builder.Length > 0)
                        {
                            builder.Length--; // 去掉最后一个字符:
                        }

                        SelectedText = builder == null ? string.Empty : builder.ToString();
                    }
                }
            }
        }

        public void Handle(DomainAddEvent message)
        {
            var domainName = message.Name;
            var domain = new Domain() { Name = domainName };
            var domainEx = new DomainEx(domain);
            // 插入到倒数第二项  
            if (DomainExs.Count > 1)
            {
                DomainExs.Insert(DomainExs.Count - 1, domainEx);
            }
            else
            {
                // 如果列表中少于两个项，可以选择添加到末尾或根据需要处理
                DomainExs.Add(domainEx);
            }

            reload_ItemsSource();
        }

        public void Handle(DomainModifyEvent message)
        {
            // 修改前的名称
            var domainName = message.Name;

            // 修改后的名称
            var newdomainName = message.newName;

            for (int i = 0; i < DomainExs.Count; i++)
            {
                var domainEx = DomainExs[i];
                var isChecked = DomainExs[i].IsChecked;
                if (domainEx.Domain.Name == domainName)
                {
                    DomainExs[i] = new DomainEx(new Domain() { Name = newdomainName });
                    if (isChecked == true)
                    {
                        DomainExs[i].IsChecked = false;
                        DomainExs[i].IsChecked = true;
                    }
                }
            }
            reload_ItemsSource();
        }

        public void Handle(DomainDeleteEvent message)
        {
            var domainName = message.Name;
            for (int i = 0; i < DomainExs.Count; i++)
            {
                var domainEx = DomainExs[i];
                if (domainEx.Domain.Name == domainName)
                {
                    DomainExs[i].IsChecked = false;
                    DomainExs.Remove(DomainExs[i]);
                }
            }
            reload_ItemsSource();
        }
    }
}
