
namespace MetBench_Client.Views.Pages
{
    public partial class CandidateReviewPage
    {
        public ViewModels.CandidateReviewViewModel ViewModel { get; }

        public CandidateReviewPage(ViewModels.CandidateReviewViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
