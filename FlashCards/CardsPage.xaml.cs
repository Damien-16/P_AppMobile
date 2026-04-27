using FlashCards.Models;
using FlashCards.Services;
using System.Collections.ObjectModel;

namespace FlashCards
{
    // On re�oit le deck et la liste globale pour pouvoir sauvegarder
    [QueryProperty(nameof(CurrentDeck), "deck")]
    [QueryProperty(nameof(AllDecks), "decks")]
    public partial class CardsPage : ContentPage
    {
        public Deck CurrentDeck { get; set; }
        public ObservableCollection<Deck> AllDecks { get; set; }
        private JsonDataService _dataService = new JsonDataService();

        public CardsPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Title = $"Cartes : {CurrentDeck?.Name}";
            CardsCollectionView.ItemsSource = CurrentDeck?.Cards;
        }

        private async void OnAddCardClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FrontEntry.Text) || string.IsNullOrWhiteSpace(BackEntry.Text))
            {
                await DisplayAlert("Erreur", "Veuillez remplir les deux faces", "OK");
                return;
            }

            var newCard = new Card { Front = FrontEntry.Text, Back = BackEntry.Text };
            CurrentDeck.Cards.Add(newCard);

            // On notifie que le nombre de cartes a chang pour l'cran prcdent
            CurrentDeck.RefreshCardCount();

            await SaveData();

            FrontEntry.Text = string.Empty;
            BackEntry.Text = string.Empty;
        }

        private async void OnStudyClicked(object sender, EventArgs e)
        {
            if (CurrentDeck == null || CurrentDeck.Cards.Count == 0)
            {
                await DisplayAlert("Info", "Ajoutez des cartes avant d'apprendre !", "OK");
                return;
            }

            var navigationParameter = new Dictionary<string, object>
            {
                { "deck", CurrentDeck }
            };
            await Shell.Current.GoToAsync("StudyPage", navigationParameter);
        }

        private async void OnDeleteCardClicked(object sender, EventArgs e)
        {
            var card = (sender as Button)?.CommandParameter as Card;
            if (card != null && CurrentDeck.Cards.Remove(card))
            {
                CurrentDeck.RefreshCardCount();
                await SaveData();
            }
        }

        private async Task SaveData()
        {
            await _dataService.SaveDecksAsync(AllDecks.ToList());
        }

        private async void OnBackClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("..");
    }
}