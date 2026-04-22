using FlashCards.Models;
using FlashCards.Services;
using System.Collections.ObjectModel;

namespace FlashCards
{
    public partial class DecksPage : ContentPage
    {
        private JsonDataService _dataService;
        private ObservableCollection<Deck> _decks;
        private int _nextId = 1;

        public DecksPage()
        {
            InitializeComponent();
            _dataService = new JsonDataService();
            _decks = new ObservableCollection<Deck>();
            LoadDecks();
        }

        private async void LoadDecks()
        {
            List<Deck> loadedDecks = await _dataService.LoadDecksAsync();

            _decks.Clear();
            foreach (Deck deck in loadedDecks)
            {
                _decks.Add(deck);
            }

            if (_decks.Any())
            {
                _nextId = _decks.Max(d => d.Id) + 1;
            }

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
            string name = NewDeckEntry.Text?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                await DisplayAlert("Erreur", "Veuillez entrer un nom", "OK");
                return;
            }

            Deck newDeck = new Deck
            {
                Id = _nextId++,
                Name = name,
                CreatedDate = DateTime.Now
            };

            _decks.Add(newDeck);
            await _dataService.SaveDecksAsync(_decks.ToList());

            NewDeckEntry.Text = string.Empty;
            UpdateInfo($"Ajouté: {name}");
        }

        // Nouvelle méthode pour le clic sur le cadre entier
        private async void OnDeckTapped(object sender, EventArgs e)
        {
            // Le "sender" est le Frame sur lequel on a cliqué
            var frame = sender as Frame;
            // Le BindingContext du Frame est l'objet Deck correspondant
            var deck = frame?.BindingContext as Deck;

            if (deck == null) return;

            var parameters = new Dictionary<string, object>
            {
                { "deck", deck },
                { "decks", _decks }
            };

            await Shell.Current.GoToAsync("CardsPage", parameters);
        }

        private async void OnEditDeckClicked(object sender, EventArgs e)
        {
            Button button = sender as Button;
            Deck deck = button?.CommandParameter as Deck;

            if (deck == null) return;

            Dictionary<string, object> navigationParameter = new Dictionary<string, object>
            {
                { "deck", deck },
                { "dataService", _dataService },
                { "decks", _decks }
            };
            await Shell.Current.GoToAsync("EditDeck", navigationParameter);
        }

        private async void OnDeleteDeckClicked(object sender, EventArgs e)
        {
            Button button = sender as Button;
            Deck deck = button?.CommandParameter as Deck;

            if (deck == null) return;

            bool confirm = await DisplayAlert(
                "Confirmation",
                $"Voulez-vous vraiment supprimer '{deck.Name}' ?",
                "Supprimer",
                "Annuler"
            );

            if (!confirm) return;

            _decks.Remove(deck);
            await _dataService.SaveDecksAsync(_decks.ToList());
            UpdateInfo($"Supprimé: {deck.Name}");
        }
    }
}