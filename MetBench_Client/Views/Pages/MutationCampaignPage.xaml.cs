
namespace MetBench_Client.Views.Pages
{
    public partial class MutationCampaignPage
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
