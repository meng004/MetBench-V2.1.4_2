using CommunityToolkit.Mvvm.ComponentModel;
using MetBench_Domain;

namespace MetBench_Client.Models
{
    public partial class DomainEx : ObservableObject
    {
        public Domain Domain { get; private set; }

        [ObservableProperty]
        private bool _isChecked;

        public DomainEx(Domain domain)
        {
            Domain = domain;
        }
    }
}
