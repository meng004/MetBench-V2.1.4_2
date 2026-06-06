using CommunityToolkit.Mvvm.ComponentModel;
using MetBench_BLL;
using MetBench_Client.Views.Pages;
using MetBench_UI.Localization;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels
{
    public class MTReportGeneratorViewModel : ObservableObject, INavigationAware
    {
        // 导航服务 用于页面切换
        private INavigationService _navigationService;

        // 页面服务 用于获取页面实体
        private IPageService _pageService;

        private MTReportGeneratorService _mtReportGeneratorSerive;

        public LocalizedTextProvider Localization { get; }

        public List<string> ReportTypeList { get { return new List<string>() { "Pdf", "Word", "Excel", "Html" }; } }

        public object PreviewContent { get; set; }

        public int SelectedIndex { get; set; }

        //public string SelectedValue { get; set; }

        private string _selectedValue;
        public string SelectedValue
        {
            get => _selectedValue;
            set
            {
                if (_selectedValue == value) return; // 值未变化时不处理

                _selectedValue = value;
                OnPropertyChanged();
                _ = HandleSelectionChangeAsync(); // 提取处理逻辑到单独方法
            }
        }
        private async Task HandleSelectionChangeAsync()
        {
            try
            {
                var targetPageType = typeof(Views.Pages.MTReportGeneratorPage);
                var page = _pageService.GetPage(targetPageType) as MTReportGeneratorPage;
                var webView = page.webview2;
                switch (SelectedValue)
                {
                    case "Pdf":
                        if (PdfReportPath == string.Empty)
                        {
                            await showMessageAsync("无目标文件！", "Tips");
                            break;
                        }
                        FilePath = PdfReportPath;
                        await webView.EnsureCoreWebView2Async();
                        webView.CoreWebView2.Navigate(PdfReportPath);
                        break;
                    case "Word":
                        if (WrodReportPath == string.Empty)
                        {
                            await showMessageAsync("无目标文件！", "Tips");
                            break;
                        }
                        await showMessageAsync("暂不支持在线浏览Word文件！", "Tips");
                        FilePath = WrodReportPath;
                        break;
                    case "Excel":
                        if (ExcelReportPath == string.Empty)
                        {
                            await showMessageAsync("无目标文件！", "Tips");
                            break;
                        }
                        await showMessageAsync("暂不支持在线浏览Excel文件！", "Tips");
                        FilePath = ExcelReportPath;
                        break;
                    case "Html":
                        if (HtmlReportPath == string.Empty)
                        {
                            await showMessageAsync("无目标文件！", "Tips");
                            break;
                        }
                        FilePath = HtmlReportPath;
                        webView.Source = new Uri(HtmlReportPath);
                        break;
                }
            }
            catch
            {
                await showMessageAsync("报告预览失败！", "Tips");
            }
        }


        public string FilePath { get; set; } = string.Empty;

        // 进度条属性 IsIndeterminate
        public bool IsIndeterminate { get; set; }

        // 进度条属性 Visibility
        public Visibility Visibility { get; set; }

        ////MT报告路径
        public string HtmlReportPath { get; set; } = string.Empty;

        public string ExcelReportPath { get; set; } = string.Empty;

        public string WrodReportPath { get; set; } = string.Empty;

        public string PdfReportPath { get; set; } = string.Empty;

        public Uri Source { get; set; }

        // 页面传参标志
        public bool _isNavigate { get; set; } = false;

        public void OnNavigatedFrom()
        {

        }

        public void OnNavigatedTo()
        {
            if (_isNavigate)
            {
                SelectedIndex = 0;
                SelectedValue = "Pdf";
                FilePath = PdfReportPath;

                //var targetPageType = typeof(Views.Pages.MTReportGeneratorPage);
                //var page = _pageService.GetPage(targetPageType) as MTReportGeneratorPage;
                //var webView = page.webview2;
                //webView.Source = new Uri(HtmlReportPath);

                // webView展示PDF
                //await webView.EnsureCoreWebView2Async();
                //webView.CoreWebView2.Navigate(PdfReportPath);

            }

            _isNavigate = false;
        }

        // 构造函数
        public MTReportGeneratorViewModel(IPageService pageService, INavigationService navigationService, MTReportGeneratorService mTReportGeneratorSerive, LocalizedTextProvider localization)
        {
            _navigationService = navigationService;
            _pageService = pageService;
            _mtReportGeneratorSerive = mTReportGeneratorSerive;
            Localization = localization;
            IsIndeterminate = false;
            Visibility = Visibility.Collapsed;
        }

        // 提示信息弹窗
        public async Task<bool> showMessageAsync(string message, string title)
        {
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = title,
                Content = message,
            };
            uiMessageBox.CloseButtonAppearance = 0;
            uiMessageBox.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            uiMessageBox.CloseButtonText = "OK";
            var messageResult = (await uiMessageBox.ShowDialogAsync()).ToString();

            // primary为第一个按钮
            return messageResult == "Primary" ? true : false;
        }

        public async Task cmb_SelectionChanged()
        {
            var targetPageType = typeof(Views.Pages.MTReportGeneratorPage);
            var page = _pageService.GetPage(targetPageType) as MTReportGeneratorPage;
            var webView = page.webview2;
            switch (SelectedValue)
            {
                case "Pdf":
                    if (PdfReportPath == string.Empty)
                    {
                        await showMessageAsync("无目标文件！", "Tips");
                        break;
                    }
                    FilePath = PdfReportPath;
                    await webView.EnsureCoreWebView2Async();
                    webView.CoreWebView2.Navigate(PdfReportPath);
                    break;
                case "Word":
                    if (WrodReportPath == string.Empty)
                    {
                        await showMessageAsync("无目标文件！", "Tips");
                        break;
                    }
                    await showMessageAsync("暂不支持在线浏览Word文件！", "Tips");
                    FilePath = WrodReportPath;
                    break;
                case "Excel":
                    if (ExcelReportPath == string.Empty)
                    {
                        await showMessageAsync("无目标文件！", "Tips");
                        break;
                    }
                    await showMessageAsync("暂不支持在线浏览Excel文件！", "Tips");
                    FilePath = ExcelReportPath;
                    break;
                case "Html":
                    if (HtmlReportPath == string.Empty)
                    {
                        await showMessageAsync("无目标文件！", "Tips");
                        break;
                    }
                    FilePath = HtmlReportPath;
                    webView.Source = new Uri(HtmlReportPath);
                    break;
            }
        }

        public async Task btn_ExportReport()
        {
            if (FilePath == string.Empty)
            {
                await showMessageAsync("指定的MT报告文件不存在！", "Tips");
                return;
            }
            var extend = Path.GetExtension(FilePath).TrimStart('.');
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Tips",
                Content = $"确认导出MT报告文件({extend})吗？",
            };
            uiMessageBox.CloseButtonAppearance = 0;
            uiMessageBox.CloseButtonText = "No";
            uiMessageBox.IsPrimaryButtonEnabled = true;
            uiMessageBox.PrimaryButtonAppearance = 0;
            uiMessageBox.HorizontalAlignment = HorizontalAlignment.Center;
            uiMessageBox.PrimaryButtonText = "Yes";

            var messageResult = (await uiMessageBox.ShowDialogAsync()).ToString();
            //primary为第一个按钮
            var confirmResult = messageResult == "Primary" ? true : false;
            if (!confirmResult)
            {
                return;
            }


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

            try
            {
                // 2. 创建FileInfo对象
                FileInfo fileInfo = new FileInfo(FilePath);

                // 3. 验证文件是否存在（可选）
                if (!fileInfo.Exists)
                {
                    Console.WriteLine($"警告：文件不存在 {FilePath}");
                    // 仍然返回FileInfo对象，但Exists属性为false
                }

                // 5. 确保目标目录存在
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                    Console.WriteLine($"创建目标目录: {targetDirectory}");
                }

                // 6. 构建目标文件路径
                string destFilePath = Path.Combine(targetDirectory, fileInfo.Name);

                // 7. 执行文件复制（覆盖已存在的文件）
                File.Copy(fileInfo.FullName, destFilePath, overwrite: true);
                Console.WriteLine($"已复制文件到: {destFilePath}");

                await showMessageAsync($"导出MT报告文件({extend})成功！", "Tips");
            }
            catch (Exception ex)
            {
                await showMessageAsync($"导出MT报告文件({extend})失败！","Tips");
            }

        }
    }
}
