using CommunityToolkit.Mvvm.ComponentModel;
using MetBench_BLL;
using MetBench_Client.Util;
using MetBench_Client.Views.Pages;
using MetBench_Domain;
using MetBench_IDAL;
using Stylet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels
{
    public class MRDisplayViewModel : ObservableObject, INavigationAware, IHandle<ApplicationAddEvent>, IHandle<ApplicationMoidfyEvent>, IHandle<ApplicationDeleteEvent>, IHandle<DomainAddEvent>, IHandle<DomainModifyEvent>, IHandle<DomainDeleteEvent>, IHandle<MetamorphicRelationOperationEvent>
    {
        //导航服务 用于页面切换
        private INavigationService _navigationService;
        //页面服务 用于获取页面实体
        private IPageService _pageService;

        // 蜕变关系管理服务
        MetamorphicRelationService _metamorphicRelationSerive;

        // OrderOfMR_ComboBox的数据源
        public List<string> OrderOfMRList { get { return MultClass.GetOrderOfMR(); } }

        //// RtType为枚举类型 分别为 数值 谓词 其他
        //// RepresentationType_ComboBox的数据源
        //public List<string> RepresentationTypeList { get { return MultClass.GetRtTypeList(); } }

        // DataGrid控件的数据源
        public ObservableCollection<MetamorphicRelations_QueryResultData> Data { get; set; }

        // DataGrid的SelectedItem
        public MetamorphicRelations_QueryResultData DataGridSelectItem { get; set; }

        // OrderOfMR_ComboBox的SelectedIndex
        // 0表示全部关系 1表示二元关系  2表示三元关系 3多元关系
        public int OrderOfMR_ComboBoxSelectedIndex { get; set; } = 0;

        //// RepresentationType_ComboBoxSelectedIndex的SelectedIndex  1 数值 2 谓词 3 其他
        //public int RepresentationType_ComboBoxSelectedIndex { get; set; } = 0;

        // OrderOfMR_ComboBox的SelectedValue
        public string OrderOfMR_ComboBoxSelectedValue { get; set; } = string.Empty;

        // txbDimensionOfInputPattern的数据绑定属性
        public string DimensionOfInputPattern { get; set; } = string.Empty;

        // txbDimensionOfOutputPattern的数据绑定属性
        public string DimensionOfOutputPattern { get; set; } = string.Empty;

        // txbApplicationName的数据绑定属性
        public string ApplicationName { get; set; } = string.Empty;

        // txbDomainName的数据绑定属性
        public string DomainName { get; set; } = string.Empty;

        // 初始化标志
        private bool _isInitialized = false;

        // 事件聚合
        private IEventAggregator _eventAggregator;

        // 每页数据行数量
        public int DataCountPerPage { get; set; } = 5;

        // 每页最大数据行数量
        public int MaxPageCount { get; set; } = 10;

        // 页码
        public int PageIndex { get; set; } = 1;

        // 保存查询结果
        public ObservableCollection<MetamorphicRelations_QueryResultData> QueryTempData { get; set; } = new ObservableCollection<MetamorphicRelations_QueryResultData>();
        // 实现接口 INavigationAware
        public void OnNavigatedTo()
        {
        }

        public void OnNavigatedFrom()
        {
        }

        public MRDisplayViewModel(MetamorphicRelationService metamorphicRelationSerive, IPageService pageService, INavigationService navigationService, IEventAggregator eventAggregator)
        {
            _metamorphicRelationSerive = metamorphicRelationSerive;
            _pageService = pageService;
            _navigationService = navigationService;
            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
            //Init_MRs();
            this.reload_ItemsSource();

            _eventAggregator = eventAggregator;
            _isInitialized = true;
        }

        ////初始话MR数据
        //public void Init_MRs()
        //{
        //       QueryTempData = _metamorphicRelationSerive.showMultTwoTableResult();
        //}

        // 加载MR数据
        public void reload_ItemsSource()
        {
            var datas = new ObservableCollection<MetamorphicRelations_QueryResultData>();

            if (!_isInitialized) 
            {
                datas = _metamorphicRelationSerive.showMultTwoTableResult();
            }

            if ( QueryTempData.Count>0) 
            {
                datas = QueryTempData;
            }

            for (int i = 0; i < datas.Count; i++)
            {
                // 将Application对应的DomainName进行拆分，一条查询记录对应一个DomainName，并换行处理 \n
                var domainname = datas[i].DomainName;
                if (domainname == null||domainname==string.Empty)
                {
                    // 当DomainName为null或为空字符串，用“无”替代占位
                    datas[i].DomainName = "无";
                }
                else
                {
                    // 将多个DomainName进行拆分，以:为分隔符
                    string[] strarray = domainname.Split(":");

                    // strarry长度为1时，只有一个DomainName
                    if (strarray.Length > 1)
                    {
                        datas[i].DomainName = string.Empty;
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

            // 按关键字查询
            var mrs = datas.ToList();

            // 分页数量
            var count = (double)(mrs.Count * 1.0 / DataCountPerPage);

            // 正常使用需要将其解除注释
            MaxPageCount = (int)Math.Ceiling(count);

            // 按页码及每页记录数
            var booksPaged = mrs.Skip((index - 1) * DataCountPerPage).Take(DataCountPerPage).ToList();
            var paged = mrs.Skip((index - 1) * DataCountPerPage).Take(DataCountPerPage).ToList();
            Data = new ObservableCollection<MetamorphicRelations_QueryResultData>(booksPaged);

            //Data = datas;
        }

        // 查询MR的交互逻辑
        public void btnQuery_Click()
        {
            var mr = new MetamorphicRelation();

            // 确保 DimensionOfInputPattern 和 DimensionOfOutputPattern 非空时为正整数
            if (DimensionOfInputPattern != string.Empty && !IsValidPositiveInteger(DimensionOfInputPattern))
            {
                System.Windows.MessageBox.Show("DimensionOfInputPattern必须为正整数。");
                return; // 中断后续操作
            }

            if (DimensionOfOutputPattern != string.Empty && !IsValidPositiveInteger(DimensionOfOutputPattern))
            {
                System.Windows.MessageBox.Show("DimensionOfOutputPattern必须为正整数。");
                return; // 中断后续操作
            }

            if (OrderOfMR_ComboBoxSelectedIndex == 0)
            {
                mr.OrderOfMR = string.Empty;
            }
            else
            {
                mr.OrderOfMR = OrderOfMR_ComboBoxSelectedValue;
            }

            mr.DimensionOfInputPattern = DimensionOfInputPattern;
            mr.DimensionOfOutputPattern = DimensionOfOutputPattern;
            mr.InputPattern = string.Empty;
            mr.OutputPattern = string.Empty;
            mr.ApplicationName = ApplicationName;
            var domianName = DomainName;
            var datas = _metamorphicRelationSerive.QueryService(mr, domianName);

            // 对Domain进行分割
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
                        datas[i].DomainName = string.Empty;
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

            QueryTempData = datas;
            var index = PageIndex;
            // 按关键字查询
            var mrs = datas.ToList();
            // 分页数量
            var count = (double)(mrs.Count * 1.0 / DataCountPerPage);
            MaxPageCount = (int)Math.Ceiling(count);
            // 按页码及每页记录数
            var booksPaged = mrs.Skip((index - 1) * DataCountPerPage).Take(DataCountPerPage).ToList();
            var paged = mrs.Skip((index - 1) * DataCountPerPage).Take(DataCountPerPage).ToList();
            Data = new ObservableCollection<MetamorphicRelations_QueryResultData>(booksPaged);
        }

        // 辅助方法，检验是否为正整数
        private bool IsValidPositiveInteger(string input)
        {
            return int.TryParse(input, out int result) && result > 0;
        }

        // 对选中的蜕变关系进行页面跳转与传参
        public void EditSelectedMR(int id) 
        {
            var idMR = id;
            var selectedItem = Data.FirstOrDefault(x => x.IdMR == idMR);
            var targetPageType = typeof(Views.Pages.MRManagementPage);
            var page = _pageService.GetPage(targetPageType) as MRManagementPage;
            var viewModel = page.ViewModel;
            viewModel._isNavigate = true;
            viewModel.DataGridSelectItem = selectedItem;
            _navigationService.Navigate(targetPageType);
        }

        // 订阅响应ApplicationAddEvent事件
        public void Handle(ApplicationAddEvent message)
        {
            // 刷新DataGrid控件数据源
            reload_ItemsSource();
        }

        public void Handle(MetamorphicRelationOperationEvent message)
        {
            // 刷新DataGrid控件数据源
            reload_ItemsSource();
        }

        public void Handle(DomainDeleteEvent message)
        {
             // 刷新DataGrid控件数据源
            reload_ItemsSource();
        }

        public void Handle(DomainModifyEvent message)
        {
            // 刷新DataGrid控件数据源
            reload_ItemsSource();
        }

        public void Handle(DomainAddEvent message)
        {
            // 刷新DataGrid控件数据源
            reload_ItemsSource();
        }

        public void Handle(ApplicationDeleteEvent message)
        {
            // 刷新DataGrid控件数据源
            reload_ItemsSource();
        }

        public void Handle(ApplicationMoidfyEvent message)
        {
            // 刷新DataGrid控件数据源
            reload_ItemsSource();
        }
    }
}
