using FlashCards.Models;
using System.Diagnostics;

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
        private int _correctCount = 0;
        private Stopwatch _stopwatch = new Stopwatch();
        private bool _isTimerRunning = false;
        
        // Track missed cards to find "hardest"
        private Dictionary<Guid, int> _missedCount = new Dictionary<Guid, int>();

        public StudyPage()
        {
            InitializeComponent();
        }

        private void InitializeStudy()
        {
            if (CurrentDeck != null && CurrentDeck.Cards.Count > 0)
            {
                _currentIndex = 0;
                _correctCount = 0;
                _isShowingFront = true;
                _missedCount.Clear();
                
                StatsView.IsVisible = false;
                StudyControls.IsVisible = true;
                ProgressLabel.IsVisible = true;
                TimerLabel.IsVisible = true;
                CardFrame.IsVisible = true;
                
                CardFrame.RotationY = 0;
                CardFrame.TranslationX = 0;
                CardFrame.Opacity = 1;

                _stopwatch.Restart();
                _isTimerRunning = true;
                StartTimerUpdate();
                
                ShowCard();
            }
            else
            {
                DisplayAlert("Info", "Ce deck ne contient pas de cartes.", "OK");
                Shell.Current.GoToAsync("..");
            }
        }

        private void StartTimerUpdate()
        {
            Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                if (!_isTimerRunning) return false;
                TimerLabel.Text = _stopwatch.Elapsed.ToString(@"mm\:ss");
                return true;
            });
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

        private void OnCorrectClicked(object sender, EventArgs e)
        {
            _correctCount++;
            MoveToNext(true);
        }

        private void OnIncorrectClicked(object sender, EventArgs e)
        {
            var card = CurrentDeck.Cards[_currentIndex];
            if (!_missedCount.ContainsKey(card.Id))
                _missedCount[card.Id] = 0;
            _missedCount[card.Id]++;
            
            MoveToNext(false);
        }

        private async void MoveToNext(bool isCorrect)
        {
            // Animation: Slide out
            double translationX = isCorrect ? 500 : -500;
            await Task.WhenAll(
                CardFrame.TranslateTo(translationX, 0, 250, Easing.CubicIn),
                CardFrame.FadeTo(0, 250)
            );

            if (_currentIndex < CurrentDeck.Cards.Count - 1)
            {
                _currentIndex++;
                _isShowingFront = true;
                ShowCard();

                // Animation: Slide in
                CardFrame.TranslationX = -translationX;
                await Task.WhenAll(
                    CardFrame.TranslateTo(0, 0, 250, Easing.CubicOut),
                    CardFrame.FadeTo(1, 250)
                );
            }
            else
            {
                _isTimerRunning = false;
                _stopwatch.Stop();
                ShowStats();
            }
        }

        private void ShowStats()
        {
            StudyControls.IsVisible = false;
            ProgressLabel.IsVisible = false;
            TimerLabel.IsVisible = false;
            CardFrame.IsVisible = false;
            StatsView.IsVisible = true;

            // Format time like "1 m 55s"
            string timeStr = "";
            if (_stopwatch.Elapsed.TotalMinutes >= 1)
                timeStr = $"{(int)_stopwatch.Elapsed.TotalMinutes} m {_stopwatch.Elapsed.Seconds}s";
            else
                timeStr = $"{_stopwatch.Elapsed.Seconds}s";
                
            TimeLabel.Text = timeStr;
            
            double percentage = (double)_correctCount / CurrentDeck.Cards.Count * 100;
            PercentageLabel.Text = $"{Math.Round(percentage)}%";

            // Find hardest card
            if (_missedCount.Any())
            {
                var hardestId = _missedCount.OrderByDescending(x => x.Value).First().Key;
                var hardestCard = CurrentDeck.Cards.FirstOrDefault(c => c.Id == hardestId);
                HardestCardLabel.Text = hardestCard?.Front ?? "---";
            }
            else
            {
                HardestCardLabel.Text = "Aucune !";
            }
        }

        private void OnRestartClicked(object sender, EventArgs e)
        {
            InitializeStudy();
        }

        private async void OnBackToDecksClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///DecksPage");
        }
    }
}