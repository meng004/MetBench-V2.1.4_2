using CommunityToolkit.Mvvm.ComponentModel;
using MetBench_Domain;

namespace MetBench_Client.Models
{
    public class ApplicationEx : ObservableObject
    {
        public Application Application { get; private set; }

        private bool _isChecked;

        public bool IsChecked
        {
            get
            {
                return _isChecked;
            }

            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;

                    OnPropertyChanged("IsChecked");
                }
            }
        }

        public ApplicationEx(Application application)
        {
            Application = application;
        }

        // WPF ComboBox 等控件 ToString() 显示文本; 委托给底层 Application
        // (Windows UAT round-1 limeng 2026-05-18 UC-A5/B1 bug fix)
        public override string ToString() => Application?.ToString() ?? string.Empty;
    }
}
