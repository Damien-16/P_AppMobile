namespace FlashCards
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Enregistrement de la route pour la page de modification
            Routing.RegisterRoute("EditDeck", typeof(EditDeckPage));
        }
    }
}