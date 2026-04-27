using FlashCards.Models;
using FlashCards.Services;
using System.Collections.ObjectModel;

namespace FlashCards
{
    public partial class DecksPage : ContentPage
    {
        private JsonDataService _dataService;
        private List<Deck> _decks;
        private List<Deck> _filteredDecks;

        private int _nextId = 1;

        public DecksPage()
        {
            InitializeComponent();
            _dataService = new JsonDataService();
            _decks = new List<Deck>();
            _filteredDecks = new List<Deck>();

            LoadDecks();
        }

        private async void LoadDecks()
        {
            List<Deck> loadedDecks = await _dataService.LoadDecksAsync();
            _decks = loadedDecks ?? new List<Deck>();

            if (_decks.Any())
            {
                _nextId = _decks.Max(d => d.Id) + 1;
            }

            ApplyFilter();
            UpdateInfo($"Charg�: {_decks.Count} deck(s)");
        }

        private void ApplyFilter()
        {
            string search = SearchEntry?.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(search))
            {
                _filteredDecks = _decks.ToList();
            }
            else
            {
                _filteredDecks = _decks
                    .Where(d => d.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            DecksCollectionView.ItemsSource = null;
            DecksCollectionView.ItemsSource = _filteredDecks;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
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
            await _dataService.SaveDecksAsync(_decks);

            NewDeckEntry.Text = string.Empty;
            ApplyFilter(); // Rafra�chir l'affichage
            UpdateInfo($"Ajout�: {name}");
        }

        private async void OnDeckTapped(object sender, EventArgs e)
        {
            var frame = sender as Frame;
            var deck = frame?.BindingContext as Deck;

            if (deck == null) return;

            // ATTENTION : Si CardsPage attend une ObservableCollection, 
            // il faut la crer ici, sinon la navigation va crasher !
            var parameters = new Dictionary<string, object>
            {
                { "deck", deck },
                { "decks", new ObservableCollection<Deck>(_decks) }
            };

            await Shell.Current.GoToAsync("CardsPage", parameters);
        }

        private async void OnStudyDeckClicked(object sender, EventArgs e)
        {
            Button button = sender as Button;
            Deck deck = button?.CommandParameter as Deck;

            if (deck == null) return;

            if (deck.Cards.Count == 0)
            {
                await DisplayAlert("Info", "Ajoutez des cartes avant d'apprendre !", "OK");
                return;
            }

            var navigationParameter = new Dictionary<string, object>
            {
                { "deck", deck }
            };
            await Shell.Current.GoToAsync("StudyPage", navigationParameter);
        }

        private async void OnEditDeckClicked(object sender, EventArgs e)
        {
            Button button = sender as Button;
            Deck deck = button?.CommandParameter as Deck;

            if (deck == null) return;

            var navigationParameter = new Dictionary<string, object>
            {
                { "deck", deck },
                { "dataService", _dataService },
                { "decks", new ObservableCollection<Deck>(_decks) }
            };
            await Shell.Current.GoToAsync("EditDeck", navigationParameter);
        }

        private async void OnDeleteDeckClicked(object sender, EventArgs e)
        {
            Button button = sender as Button;
            Deck deck = button?.CommandParameter as Deck;

            if (deck == null) return;

            bool confirm = await DisplayAlert("Confirmation", $"Supprimer '{deck.Name}' ?", "Supprimer", "Annuler");
            if (!confirm) return;

            _decks.Remove(deck);
            await _dataService.SaveDecksAsync(_decks);
            ApplyFilter(); // Rafra�chir l'affichage
            UpdateInfo($"Supprim�: {deck.Name}");
        }
    }
}