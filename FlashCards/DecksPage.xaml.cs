using FlashCards.Models;
using FlashCards.Services;
using System.Collections.ObjectModel; 


namespace FlashCards
{
    public partial class DecksPage : ContentPage
    {
        private JsonDataService _dataService;
        private ObservableCollection<Deck> _decks;  // List devient ObservableCollection
        private int _nextId = 1;

        public DecksPage()
        {
            InitializeComponent();
            _dataService = new JsonDataService();
            _decks = new ObservableCollection<Deck>();  // new ObservableCollection
            LoadDecks();
        }

        private async void LoadDecks()
        {
            List<Deck> loadedDecks = await _dataService.LoadDecksAsync();

            // Clear and repopulate ObservableCollection
            _decks.Clear();
            foreach (Deck deck in loadedDecks)
            {
                _decks.Add(deck);
            }

            if (_decks.Any())
            {
                _nextId = _decks.Max(d => d.Id) + 1;
            }

            // Assign ItemsSource ONCE (no need to reassign every time)
            if (DecksCollectionView.ItemsSource == null)
            {
                DecksCollectionView.ItemsSource = _decks;
            }

            UpdateInfo($"Chargé: {_decks.Count} deck(s)");
        }

        private void UpdateInfo(string message)
        {
            InfoLabel.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
        }
        private async void OnAddDeckClicked(object sender, EventArgs e)
        {
            string? name = NewDeckEntry.Text?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                await DisplayAlert("Erreur", "Veuillez entrer un nom", "OK");
                return;
            }

            Deck newDeck = new Deck
            {
                Id = _nextId++,
                Name = name,
                CardCount = 0
            };

            _decks.Add(newDeck);  // ? La vue se met à jour automatiquement !
            await _dataService.SaveDecksAsync(_decks.ToList());

            // RefreshView();  ? SUPPRIMÉ !
            NewDeckEntry.Text = string.Empty;
            UpdateInfo($"Ajouté: {name}");
        }
        private async void OnEditDeckClicked(object sender, EventArgs e)
        {
            Button? button = sender as Button;
            Deck? deck = button?.CommandParameter as Deck;

            if (deck == null) return;

            // Navigate to edit page using Shell
            // Pass deck, dataService and decks list so EditDeckPage can save
            Dictionary<string, object> navigationParameter = new Dictionary<string, object>
    {
        { "deck", deck },
        { "dataService", _dataService },
        { "decks", _decks }
    };
            await Shell.Current.GoToAsync("EditDeck", navigationParameter);
        }

        // Refresh view when returning from edit page
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // RefreshView();  ? SUPPRIMÉ !
            // L'ObservableCollection met déjà à jour la vue automatiquement
        }
        private async void OnDeleteDeckClicked(object sender, EventArgs e)
        {
            Button? button = sender as Button;
            Deck? deck = button?.CommandParameter as Deck;

            if (deck == null) return;

            bool confirm = await DisplayAlert(
                "Confirmation",
                $"Voulez-vous vraiment supprimer '{deck.Name}' ?",
                "Supprimer",
                "Annuler"
            );

            if (!confirm) return;

            _decks.Remove(deck);  // ? La vue se met à jour automatiquement !
            await _dataService.SaveDecksAsync(_decks.ToList());

            // RefreshView();  ? SUPPRIMÉ !
            UpdateInfo($"Supprimé: {deck.Name}");
        }
    }


}