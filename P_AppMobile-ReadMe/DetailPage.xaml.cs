using P_AppMobile_ReadMe.Models;
using VersOne.Epub;
using System.Text.RegularExpressions;

namespace P_AppMobile_ReadMe;

[QueryProperty(nameof(SelectedBook), "SelectedBook")]
public partial class DetailPage : ContentPage
{
    private Book _selectedBook;
    public Book SelectedBook
    {
        get => _selectedBook;
        set
        {
            _selectedBook = value;
            OnPropertyChanged();
            // Charger le contenu dès que l'objet livre est reçu
            LoadEpubText();
        }
    }

    public DetailPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    private async void LoadEpubText()
    {
        if (SelectedBook == null || string.IsNullOrEmpty(SelectedBook.FilePath)) return;

        try
        {
            // 1. Ouvrir le livre EPUB
            var epubBook = await EpubReader.ReadBookAsync(SelectedBook.FilePath);

            // 2. Extraire et concaténer le texte de tous les chapitres
            string fullText = "";
            foreach (var localTextContentFile in epubBook.ReadingOrder)
            {
                // On récupère le contenu HTML du chapitre
                string htmlContent = localTextContentFile.Content;

                // On enlève les balises HTML pour avoir du texte brut (comme votre exemple)
                string plainText = Regex.Replace(htmlContent, "<.*?>", string.Empty);

                fullText += plainText + "\n\n";
            }

            // 3. Afficher le texte dans la Label
            BookContentLabel.Text = fullText.Trim();
        }
        catch (Exception ex)
        {
            BookContentLabel.Text = "Erreur lors de la lecture du fichier EPUB.";
            await DisplayAlert("Erreur", ex.Message, "OK");
        }
    }
}