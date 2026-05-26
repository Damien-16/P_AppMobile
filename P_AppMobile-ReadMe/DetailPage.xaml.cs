using P_AppMobile_ReadMe.Models;
using P_AppMobile_ReadMe.Services;
using VersOne.Epub;
using System.Text.RegularExpressions;

namespace P_AppMobile_ReadMe;

[QueryProperty(nameof(SelectedBook), "SelectedBook")]
public partial class DetailPage : ContentPage
{
    private readonly BookService _bookService = new BookService();
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

    private List<string> _pages = new List<string>();
    private int _currentPageIndex = 0;
    private const int CharsPerPage = 1200; // Ajustable en fonction de la taille de l'écran

    private string _currentPageText;
    public string CurrentPageText
    {
        get => _currentPageText;
        set { _currentPageText = value; OnPropertyChanged(); }
    }

    private double _progressValue;
    public double ProgressValue
    {
        get => _progressValue;
        set { _progressValue = value; OnPropertyChanged(); }
    }

    private string _pageIndicator;
    public string PageIndicator
    {
        get => _pageIndicator;
        set { _pageIndicator = value; OnPropertyChanged(); }
    }

    private bool _isCoverVisible;
    public bool IsCoverVisible
    {
        get => _isCoverVisible;
        set { _isCoverVisible = value; OnPropertyChanged(); }
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

            // Paginer le texte
            fullText = fullText.Trim();
            PaginateText(fullText);
        }
        catch (Exception ex)
        {
            CurrentPageText = "Erreur lors de la lecture du fichier EPUB.";
            await DisplayAlert("Erreur", ex.Message, "OK");
        }
    }

    private void PaginateText(string fullText)
    {
        _pages.Clear();
        
        // Séparer le texte par paragraphes (en utilisant les retours à la ligne)
        // Le nettoyage HTML a déjà remplacé les blocs par des sauts de ligne
        string[] paragraphs = fullText.Split(new[] { "\n\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        string currentPageText = "";
        
        foreach (var paragraph in paragraphs)
        {
            // Si on dépasse la limite et qu'on a déjà du texte sur la page, on crée une nouvelle page
            if (currentPageText.Length + paragraph.Length > CharsPerPage && currentPageText.Length > 0)
            {
                _pages.Add(currentPageText.Trim());
                currentPageText = paragraph + "\n\n";
            }
            else
            {
                currentPageText += paragraph + "\n\n";
            }
        }
        
        // Ajouter le texte restant comme dernière page
        if (!string.IsNullOrWhiteSpace(currentPageText))
        {
            _pages.Add(currentPageText.Trim());
        }
        if (_pages.Count == 0)
        {
            _pages.Add("");
        }

        // Reprise automatique : on récupère l'index sauvegardé
        _currentPageIndex = SelectedBook.LastPageRead;

        UpdatePageDisplay();
    }

    private void UpdatePageDisplay()
    {
        if (_pages.Count == 0) return;

        CurrentPageText = _pages[_currentPageIndex];
        PageIndicator = $"{_currentPageIndex + 1}/{_pages.Count}";
        ProgressValue = _pages.Count > 1 ? (double)_currentPageIndex / (_pages.Count - 1) : 1.0;
        IsCoverVisible = _currentPageIndex == 0;

        // Mémorisation de la page actuelle
            SelectedBook.LastPageRead = _currentPageIndex;
            SaveProgress();
        
    }

    private async void SaveProgress()
    {
        // On charge la liste complète pour mettre à jour le bon livre et sauvegarder le tout
        var books = await _bookService.LoadBooksAsync();
        var bookToUpdate = books.FirstOrDefault(b => b.Id == SelectedBook.Id);
        
        if (bookToUpdate != null)
        {
            bookToUpdate.LastPageRead = _currentPageIndex;
            await _bookService.SaveBooksAsync(books);
        }
    }

    private void OnPreviousClicked(object sender, EventArgs e)
    {
        if (_currentPageIndex > 0)
        {
            _currentPageIndex--;
            UpdatePageDisplay();
        }
    }

    private void OnNextClicked(object sender, EventArgs e)
    {
        if (_currentPageIndex < _pages.Count - 1)
        {
            _currentPageIndex++;
            UpdatePageDisplay();
        }
    }

    private async void OnBackTapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private bool _isTagsPanelVisible;
    public bool IsTagsPanelVisible
    {
        get => _isTagsPanelVisible;
        set { _isTagsPanelVisible = value; OnPropertyChanged(); }
    }

    private int _populatingTagsVersion = 0;

    private void OnTagsButtonClicked(object sender, TappedEventArgs e)
    {
        IsTagsPanelVisible = true;
        PopulateBookTags();
    }

    private void OnCloseTagsPanelClicked(object sender, EventArgs e)
    {
        IsTagsPanelVisible = false;
    }

    private async void OnAddTagClicked(object sender, EventArgs e)
    {
        var newTag = NewTagEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(newTag)) return;

        if (SelectedBook != null)
        {
            var currentBook = SelectedBook;
            NewTagEntry.Text = string.Empty; // Clear immediately
            
            try
            {
                await _bookService.AddTagToBookAsync(currentBook.Id, newTag);
                if (SelectedBook == currentBook)
                {
                    if (currentBook.Tags == null)
                    {
                        currentBook.Tags = new List<string>();
                    }
                    if (!currentBook.Tags.Contains(newTag, StringComparer.OrdinalIgnoreCase))
                    {
                        currentBook.Tags.Add(newTag);
                    }
                    PopulateBookTags();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding new tag: {ex.Message}");
            }
        }
    }

    private async void PopulateBookTags()
    {
        if (SelectedBook == null) return;
        var currentBook = SelectedBook;

        // Increment version to discard outdated async calls
        int version = ++_populatingTagsVersion;

        BookTagsFlexLayout.Children.Clear();
        AllTagsFlexLayout.Children.Clear();
        
        // 1. Populate current book's tags
        if (currentBook.Tags != null)
        {
            foreach (var tag in currentBook.Tags.ToList())
            {
                var border = new Border
                {
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(12) },
                    Stroke = Brush.Transparent,
                    BackgroundColor = Color.FromArgb("#F0EEFF"),
                    Padding = new Thickness(10, 5),
                    Margin = new Thickness(4),
                    HorizontalOptions = LayoutOptions.Start
                };

                var layout = new HorizontalStackLayout { Spacing = 5 };
                
                var label = new Label 
                { 
                    Text = tag, 
                    TextColor = Color.FromArgb("#512BD4"), 
                    FontSize = 13,
                    VerticalOptions = LayoutOptions.Center 
                };
                
                var deleteBtn = new Label 
                { 
                    Text = "✕", 
                    TextColor = Colors.Red, 
                    FontSize = 13, 
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center,
                    Margin = new Thickness(3, 0, 0, 0)
                };

                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) =>
                {
                    if (SelectedBook != currentBook || _populatingTagsVersion != version) return;
                    
                    deleteBtn.IsEnabled = false;
                    try
                    {
                        await _bookService.RemoveTagFromBookAsync(currentBook.Id, tag);
                        if (SelectedBook == currentBook)
                        {
                            currentBook.Tags.Remove(tag);
                            PopulateBookTags();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error removing tag: {ex.Message}");
                    }
                };
                deleteBtn.GestureRecognizers.Add(tapGesture);

                layout.Children.Add(label);
                layout.Children.Add(deleteBtn);
                border.Content = layout;

                BookTagsFlexLayout.Children.Add(border);
            }
        }

        // 2. Populate all other available tags in the library
        try
        {
            var allTags = await _bookService.GetAllTagsAsync();
            
            // Check if the user closed the panel, switched books, or started another load after the await
            if (SelectedBook != currentBook || version != _populatingTagsVersion) return;

            var currentBookTags = currentBook.Tags ?? new List<string>();
            var otherTags = allTags.Where(t => !currentBookTags.Contains(t, StringComparer.OrdinalIgnoreCase)).ToList();

            foreach (var tag in otherTags)
            {
                var border = new Border
                {
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(12) },
                    Stroke = Color.FromArgb("#512BD4"),
                    StrokeThickness = 1,
                    BackgroundColor = Colors.White,
                    Padding = new Thickness(10, 5),
                    Margin = new Thickness(4),
                    HorizontalOptions = LayoutOptions.Start
                };

                var label = new Label 
                { 
                    Text = tag, 
                    TextColor = Color.FromArgb("#512BD4"), 
                    FontSize = 13,
                    VerticalOptions = LayoutOptions.Center 
                };

                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) =>
                {
                    if (SelectedBook != currentBook || _populatingTagsVersion != version) return;
                    
                    border.IsEnabled = false;
                    try
                    {
                        await _bookService.AddTagToBookAsync(currentBook.Id, tag);
                        if (SelectedBook == currentBook)
                        {
                            if (currentBook.Tags == null)
                            {
                                currentBook.Tags = new List<string>();
                            }
                            if (!currentBook.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                            {
                                currentBook.Tags.Add(tag);
                            }
                            PopulateBookTags();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error adding tag: {ex.Message}");
                    }
                };
                border.GestureRecognizers.Add(tapGesture);
                border.Content = label;

                AllTagsFlexLayout.Children.Add(border);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading all tags: {ex.Message}");
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