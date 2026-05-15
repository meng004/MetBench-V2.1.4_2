using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Pages
{
    public partial class CandidateReviewPage : INavigableView<ViewModels.CandidateReviewViewModel>
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
