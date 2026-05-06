namespace FlashCards
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnManageDecksClicked(object sender, EventArgs e)
        {
            // Navigation vers la page des decks
            await Shell.Current.GoToAsync("//DecksPage");
        }

        private async void OnLearnClicked(object sender, EventArgs e)
        {
            // Navigation vers DecksPage aussi pour choisir quoi apprendre
            await Shell.Current.GoToAsync("//DecksPage");
        }
    }
}