using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FlashCards.Models
{
    public class Deck : INotifyPropertyChanged
    {
        private string _name;
        public int Id { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        // Liste des cartes du deck
        public ObservableCollection<Card> Cards { get; set; } = new ObservableCollection<Card>();

        // Propriété calculée pour afficher le nombre de cartes
        public int CardCount => Cards?.Count ?? 0;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Méthode pour forcer la mise à jour de l'affichage du nombre de cartes
        public void RefreshCardCount()
        {
            OnPropertyChanged(nameof(CardCount));
        }
    }
}