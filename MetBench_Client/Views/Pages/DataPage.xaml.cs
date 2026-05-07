using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Pages
{
    /// <summary>
    /// Interaction logic for DataView.xaml
    /// </summary>
    public partial class DataPage : INavigableView<ViewModels.DataViewModel>
    {
        public ViewModels.DataViewModel ViewModel
        {
            get;
        }

        public DataPage(ViewModels.DataViewModel viewModel)
        {
            ViewModel = viewModel;
            //数据上下文初始化赋值
            DataContext = this;
            InitializeComponent();
        }
    }
}
