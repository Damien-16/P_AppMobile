using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FlashCards.Models
{
    public class Deck : INotifyPropertyChanged
    {
        private string _name;
        private int _cardCount;

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

        public int CardCount
        {
            get => _cardCount;
            set
            {
                _cardCount = value;
                OnPropertyChanged();
            }
        }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}