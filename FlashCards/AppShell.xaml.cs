namespace FlashCards
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("EditDeck", typeof(EditDeckPage));
            Routing.RegisterRoute("CardsPage", typeof(CardsPage)); // Nouvelle route
        }
    }
}