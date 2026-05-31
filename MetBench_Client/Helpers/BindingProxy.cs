using System.Windows;

namespace MetBench_Client.Helpers
{
    /// <summary>
    /// A <see cref="Freezable"/>-based proxy that allows DataContext bindings to be
    /// used inside elements that are not part of the visual tree (e.g. DataGridColumn headers).
    /// </summary>
    public sealed class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore() => new BindingProxy();

        public object Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(object),
                typeof(BindingProxy),
                new UIPropertyMetadata(null));
    }
}
