namespace P_AppMobile_ReadMe
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnBookTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("DetailsPage");
        }

    }

}
