using System;
using System.Windows.Navigation;

namespace MetBench_Client.Helpers
{
    [Serializable]
    public class MyCustomContentState : CustomContentState
    {
        string dateCreated;
        string PageTitle;

        public MyCustomContentState(string dateCreated, string PageTitle)
        {
            this.dateCreated = dateCreated;
            this.PageTitle = PageTitle;
        }

        public override string JournalEntryName
        {
            get
            {
                return "Journal Entry " + this.dateCreated;
            }
        }

        public override void Replay(NavigationService navigationService, NavigationMode mode)
        {
            this.PageTitle += this.dateCreated;
        }
    }
}
