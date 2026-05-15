using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Pages
{
    public partial class MutationCampaignPage : INavigableView<ViewModels.MutationCampaignViewModel>
    {
        public ViewModels.MutationCampaignViewModel ViewModel { get; }

        public MutationCampaignPage(ViewModels.MutationCampaignViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
