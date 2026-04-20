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
            // Navigue vers la page enregistrée dans le Shell sous la route "DecksPage"
            await Shell.Current.GoToAsync("//DecksPage");
        }

        private async void OnLearnClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Info", "Fonctionnalité d'apprentissage bientôt disponible !", "OK");
        }
    }
}