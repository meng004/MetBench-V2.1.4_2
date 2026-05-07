using CommunityToolkit.Mvvm.ComponentModel;
using MetBench_Domain;

namespace MetBench_Client.Models
{
    public class DomainEx : ObservableObject
    {
        public Domain Domain { get; private set; }

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

        public DomainEx(Domain domain)
        {
            Domain = domain;
        }
    }
}
