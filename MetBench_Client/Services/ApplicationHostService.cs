using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MetBench_Client.Services
{
    /// <summary>
    /// Managed host of the application.
    /// 托管服务
    /// </summary>
    public class ApplicationHostService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;// 用于访问依赖注入的服务提供程序
        private IClientNavigationWindow? _navigationWindow;// 导航窗口实例

        public ApplicationHostService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// 在应用程序启动后立即执行，且只执行一次
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await HandleActivationAsync();// 异步处理激活过程
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// 在应用程序停止之前执行，可以实现一个计划服务，实现一个定期任务
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;// 返回一个已完成的任务
        }

        /// <summary>
        /// Creates main window during activation.
        /// </summary>
        private async Task HandleActivationAsync()
        {
            await Task.CompletedTask; // 检查是否已存在主窗口实例，如果不存在，则创建并显示主窗口

            if (!Application.Current.Windows.OfType<Views.Windows.MainWindow>().Any())
            { // 获取导航窗口实例，并显示窗口
                _navigationWindow = (_serviceProvider.GetService(typeof(IClientNavigationWindow)) as IClientNavigationWindow)!;
                _navigationWindow!.ShowWindow();
                // 导航到 DashboardPage 页面
                _navigationWindow.Navigate(typeof(Views.Pages.MRDisplayPage));
            }

            await Task.CompletedTask;
        }
    }
}
