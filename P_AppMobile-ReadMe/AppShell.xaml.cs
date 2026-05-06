namespace P_AppMobile_ReadMe
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("DetailsPage", typeof(DetailPage));
        }
    }
}
