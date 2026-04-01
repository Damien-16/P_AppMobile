using FlashCards.Models;
using FlashCards.Services;
using System.Collections.ObjectModel;

namespace FlashCards
{
    public partial class EditDeckPage : ContentPage, IQueryAttributable
    {
        private Deck _deck;
        private int _cardCount;
        private JsonDataService _dataService;
        private ObservableCollection<Deck> _decks;


        public EditDeckPage()
        {
            InitializeComponent();
        }

        // Receive navigation parameters
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("deck", out object? deckObj) && deckObj is Deck deck)
            {
                _deck = deck;
                _cardCount = deck.CardCount;

                // Initialize fields
                NameEntry.Text = deck.Name;
                CardCountLabel.Text = _cardCount.ToString();
            }

            if (query.TryGetValue("dataService", out object? serviceObj) && serviceObj is JsonDataService service)
            {
                _dataService = service;
            }

            if (query.TryGetValue("decks", out object? decksObj) && decksObj is ObservableCollection<Deck> decks)
            {
                _decks = decks;
            }
        }

        private void OnIncrementClicked(object sender, EventArgs e)
        {
            _cardCount++;
            CardCountLabel.Text = _cardCount.ToString();
        }

        private void OnDecrementClicked(object sender, EventArgs e)
        {
            if (_cardCount > 0)
            {
                _cardCount--;
                CardCountLabel.Text = _cardCount.ToString();
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            string? newName = NameEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(newName))
            {
                await DisplayAlert("Erreur", "Le nom ne peut pas être vide", "OK");
                return;
            }

            // Update deck
            _deck.Name = newName;
            _deck.CardCount = _cardCount;

            // Save immediately to JSON
            await _dataService.SaveDecksAsync(_decks.ToList());

            await Shell.Current.GoToAsync("..");
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}