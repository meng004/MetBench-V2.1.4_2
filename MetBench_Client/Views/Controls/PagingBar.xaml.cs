using System.Windows.Controls;

namespace MetBench_Client.Views.Controls
{
    /// <summary>
    /// Reusable paging toolbar. Binds entirely to inherited DataContext —
    /// 调用方必须确保 DataContext 是 <c>MetBench_BLL.Paging.PagingViewModel&lt;T&gt;</c> 派生。
    /// </summary>
    public partial class PagingBar : UserControl
    {
        public PagingBar()
        {
            InitializeComponent();
        }
    }
}
