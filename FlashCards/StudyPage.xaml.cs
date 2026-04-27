using FlashCards.Models;

namespace FlashCards
{
    [QueryProperty(nameof(CurrentDeck), "deck")]
    public partial class StudyPage : ContentPage
    {
        private Deck _currentDeck;
        public Deck CurrentDeck
        {
            get => _currentDeck;
            set
            {
                _currentDeck = value;
                OnPropertyChanged();
                InitializeStudy();
            }
        }

        private int _currentIndex = 0;
        private bool _isShowingFront = true;

        public StudyPage()
        {
            InitializeComponent();
        }

        private void InitializeStudy()
        {
            if (CurrentDeck != null && CurrentDeck.Cards.Count > 0)
            {
                _currentIndex = 0;
                ShowCard();
            }
            else
            {
                DisplayAlert("Info", "Ce deck ne contient pas de cartes.", "OK");
                Shell.Current.GoToAsync("..");
            }
        }

        private void ShowCard()
        {
            if (CurrentDeck == null || CurrentDeck.Cards.Count == 0) return;

            var card = CurrentDeck.Cards[_currentIndex];
            CardTextLabel.Text = _isShowingFront ? card.Front : card.Back;
            SideLabel.Text = _isShowingFront ? "Recto" : "Verso";
            ProgressLabel.Text = $"Carte {_currentIndex + 1} / {CurrentDeck.Cards.Count}";
        }

        private void OnFlipClicked(object sender, EventArgs e)
        {
            _isShowingFront = !_isShowingFront;
            ShowCard();
        }

        private void OnNextClicked(object sender, EventArgs e)
        {
            if (CurrentDeck == null) return;

            if (_currentIndex < CurrentDeck.Cards.Count - 1)
            {
                _currentIndex++;
                _isShowingFront = true;
                ShowCard();
            }
            else
            {
                DisplayAlert("Fin", "Vous avez terminé toutes les cartes de ce deck !", "OK");
                Shell.Current.GoToAsync("..");
            }
        }

        private void OnPreviousClicked(object sender, EventArgs e)
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                _isShowingFront = true;
                ShowCard();
            }
        }
    }
}