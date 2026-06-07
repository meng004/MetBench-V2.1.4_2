using System.Threading.Tasks;
using System.Windows;

namespace MetBench_Client.Services;

public static class UiDialog
{
    public static Task<bool> ShowMessageAsync(string message, string title)
    {
        ShowMessage(message, title);
        return Task.FromResult(true);
    }

    public static bool ShowMessage(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        return true;
    }

    public static bool Confirm(string message, string title)
    {
        return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    public static Task<bool> ConfirmAsync(string message, string title)
    {
        return Task.FromResult(Confirm(message, title));
    }
}
