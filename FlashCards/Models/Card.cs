namespace FlashCards.Models
{
    public class Card
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Front { get; set; }
        public string Back { get; set; }
    }
}