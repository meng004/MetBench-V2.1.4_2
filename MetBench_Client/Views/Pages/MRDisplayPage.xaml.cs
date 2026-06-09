using MetBench_Client.ViewModels;
using MetBench_Domain;
using MetBench_IDAL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;

namespace MetBench_Client.Views.Pages
{
    /// <summary>
    /// MRDisplayPage.xaml 的交互逻辑
    /// </summary>
    public partial class MRDisplayPage : Page
    {
        private INavigationService _navigationService;
        private IPageService _pageService;

        public MRDisplayPage(INavigationService navigationService, IPageService pageService, ViewModels.MRDisplayViewModel viewModel)
        {
            ViewModel = viewModel;
            //数据上下文初始化赋值
            DataContext = this;
            _navigationService = navigationService;
            _pageService = pageService;
            InitializeComponent();
        }

        public MRDisplayViewModel ViewModel
        {
            get;
        }

        //记录DataGrid行数
        private void dataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = e.Row.GetIndex() + 1;
        }

        private void pagination_PageUpdated(object sender, RoutedEventArgs e) => ViewModel.reload_ItemsSource();

        //用于页面跳转（已弃用）
        //private void editMRButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var targetPageType = typeof(Views.Pages.MRManagementPage);
        //    var page = _pageService.GetPage(targetPageType) as MRManagementPage;
        //    var dataGridSelectedItem =  dataGrid.SelectedItem as MetamorphicRelation;
        //    page.datagrid.SelectedItem = dataGridSelectedItem;

        //    NavigationService.Navigate(targetPageType);
        //}
        //public void EditSelectedMR(int id)
        //{
        //    //var idMR = id;
        //    ////var s =
        //    //var selectedItem = new ObservableCollection<MetamorphicRelations_QueryResultDatadataGrid>() { dataGrid.ItemsSource } ;
        //    ////var selectedItem = datagrid.FirstOrDefault(x => x.IdMR == idMR);
        //    var targetPageType = typeof(Views.Pages.MRManagementPage);
        //    var page = _pageService.GetPage(targetPageType) as MRManagementPage;
        //    //page.datagrid.SelectedItem = selectedItem;
        //    //var dataGridField = page.GetType().GetField("datagrid"); // 获取datagrid字段
        //    //var dataGrid = (System.Windows.Controls.DataGrid)dataGridField.GetValue(page); // 获取datagrid控件实例
        //    //dataGrid.SelectedItem = selectedItem;
        //    _navigationService.Navigate(targetPageType);
        //}
    }
}
