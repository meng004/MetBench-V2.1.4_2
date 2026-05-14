using System.Windows;
using System.Windows.Controls;

namespace MetBench_Client.Views.Controls
{
    /// <summary>
    /// Reusable paging toolbar.
    ///
    /// 用法 1（推荐，单表）：让继承的 DataContext 直接是 PagingViewModel&lt;T&gt; 派生：
    ///     &lt;controls:PagingBar DataContext="{Binding ViewModel}" /&gt;
    ///
    /// 用法 2（多表同页）：显式给 <see cref="Target"/> 依赖属性指向不同 ViewModel：
    ///     &lt;controls:PagingBar Target="{Binding ViewModel.LeftPagingVm}" /&gt;
    ///     &lt;controls:PagingBar Target="{Binding ViewModel.RightPagingVm}" /&gt;
    ///
    /// 当 <see cref="Target"/> 被设置时，控件内部 root 的 DataContext 切到 Target，
    /// 避免两条分页条共享同一父 DataContext 互相串扰。
    /// </summary>
    public partial class PagingBar : UserControl
    {
        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register(
                nameof(Target), typeof(object), typeof(PagingBar),
                new PropertyMetadata(null, OnTargetChanged));

        public object? Target
        {
            get => GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        public PagingBar()
        {
            InitializeComponent();
        }

        private static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PagingBar bar && bar.Root != null && e.NewValue != null)
            {
                bar.Root.DataContext = e.NewValue;
            }
        }
    }
}
