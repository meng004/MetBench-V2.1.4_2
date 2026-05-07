using System;
using System.Linq;
using System.Reflection;

namespace MetBench_Client.Helpers
{
    //根据传入的页面名称，通过反射查找当前程序集中符合条件的页面类型，然后将页面名称与页面类型进行匹配，返回对应的页面类型。
    //这种动态地将页面名称映射到页面类型的方法通常用于在WPF应用程序中根据名称动态加载和显示不同的页面。
    internal sealed class NameToPageTypeConverter
    {
        private static readonly Type[] PageTypes = Assembly
        .GetExecutingAssembly()
        .GetTypes()
        .Where(t => t.Namespace?.StartsWith("Wpf.Ui.Gallery.Views.Pages") ?? false)
        .ToArray();

        public static Type? Convert(string pageName)
        {
            pageName = pageName.Trim().ToLower() + "page";

            return PageTypes.FirstOrDefault(singlePageType =>
                singlePageType.Name.Equals(pageName, StringComparison.CurrentCultureIgnoreCase)
            );
        }
    }
}
