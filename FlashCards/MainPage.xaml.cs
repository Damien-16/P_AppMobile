namespace FlashCards
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void OnMenuClicked(object sender, EventArgs e)
        {
            Shell.Current.FlyoutIsPresented = true;
        }

        private async void OnManageDecksClicked(object sender, EventArgs e)
        {
            // Navigation vers la page des decks en mode gestion
            await Shell.Current.GoToAsync("//DecksPage?mode=manage");
        }

        private async void OnLearnClicked(object sender, EventArgs e)
        {
            // Navigation vers DecksPage en mode apprentissage
            await Shell.Current.GoToAsync("//DecksPage?mode=study");
        }
    }
}