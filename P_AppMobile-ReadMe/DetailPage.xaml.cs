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
            // Charger le contenu des que l'objet livre est reçu
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
                string htmlContent = localTextContentFile.Content;

                // Nettoyage plus robuste du HTML
                string plainText = CleanHtml(htmlContent);

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

    private string CleanHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";

        // 1. Supprimer le bloc <head> et son contenu
        html = Regex.Replace(html, "<head.*?>.*?</head>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // 2. Supprimer les styles et scripts
        html = Regex.Replace(html, "<style.*?>.*?</style>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        html = Regex.Replace(html, "<script.*?>.*?</script>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // 3. Remplacer les balises de bloc par des retours à la ligne pour garder la structure
        html = Regex.Replace(html, "<p.*?>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, "<h[1-6].*?>", "\n\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, "</p>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, "</h[1-6]>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, "<div.*?>", "\n", RegexOptions.IgnoreCase);

        // 4. Supprimer toutes les autres balises HTML
        string plainText = Regex.Replace(html, "<.*?>", string.Empty, RegexOptions.Singleline);

        // 5. Décoder les entités HTML (ex: &nbsp; -> espace, &eacute; -> é)
        plainText = System.Net.WebUtility.HtmlDecode(plainText);

        // 6. Nettoyer les espaces et retours à la ligne multiples
        plainText = Regex.Replace(plainText, @"\n\s*\n", "\n\n");

        return plainText.Trim();
    }
}