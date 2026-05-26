using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;
using VersOne.Epub;
using P_AppMobile_ReadMe.Models;
using P_AppMobile_ReadMe.Services;

namespace P_AppMobile_ReadMe
{
    public partial class MainPage : ContentPage
    {
        // Déclaration du service et des listes pour l'UI
        private readonly BookService _bookService;
        public ObservableCollection<Book> Books { get; set; } = new ObservableCollection<Book>();
        public ObservableCollection<string> FilterTags { get; set; } = new ObservableCollection<string>();
        
        private List<Book> _allBooks = new List<Book>();
        private string _selectedTag = "Tous";
        private bool _isAscending = false; // Par défaut, les plus récents en premier

        private Book? _editingBook;
        public Book? EditingBook
        {
            get => _editingBook;
            set { _editingBook = value; OnPropertyChanged(); }
        }

        private bool _isTagsPanelVisible;
        public bool IsTagsPanelVisible
        {
            get => _isTagsPanelVisible;
            set { _isTagsPanelVisible = value; OnPropertyChanged(); }
        }

        public MainPage()
        {
            InitializeComponent();

            // Initialisation du service
            _bookService = new BookService();

            // Indispensable pour que le XAML puisse voir la liste "Books" et "FilterTags"
            BindingContext = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadSavedBooks();
        }

        private async void LoadSavedBooks()
        {
            try
            {
                var savedBooks = await _bookService.LoadBooksAsync();
                _allBooks = savedBooks ?? new List<Book>();

                // Mettre à jour la barre de filtres
                UpdateFilterTags();

                // Appliquer les filtres et le tri actuels
                ApplyFilterAndSort();

                // Téléchargement et extraction en arrière-plan des couvertures/fichiers manquants
                _ = Task.Run(async () =>
                {
                    foreach (var book in _allBooks.ToList())
                    {
                        if (string.IsNullOrEmpty(book.CoverImagePath) || !File.Exists(book.CoverImagePath))
                        {
                            try
                            {
                                await _bookService.EnsureBookFileCachedAsync(book);
                                // Mettre à jour l'UI sur le thread principal
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    // Mettre à jour le chemin local du livre dans _allBooks
                                    var existingAll = _allBooks.FirstOrDefault(b => b.Id == book.Id);
                                    if (existingAll != null)
                                    {
                                        existingAll.CoverImagePath = book.CoverImagePath;
                                        existingAll.FilePath = book.FilePath;
                                    }

                                    // Si le livre est actuellement visible dans la liste filtrée, le notifier
                                    var visibleBook = Books.FirstOrDefault(b => b.Id == book.Id);
                                    if (visibleBook != null)
                                    {
                                        var index = Books.IndexOf(visibleBook);
                                        if (index >= 0)
                                        {
                                            Books[index] = book;
                                        }
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error caching cover in background: {ex.Message}");
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Échec du chargement : {ex.Message}", "OK");
            }
        }

        private void OnSortClicked(object sender, EventArgs e)
        {
            _isAscending = !_isAscending;
            ApplyFilterAndSort();
        }

        private void UpdateFilterTags()
        {
            TagsFilterLayout.Children.Clear();

            // Récupérer tous les tags uniques
            var uniqueTags = _allBooks
                .Where(b => b.Tags != null)
                .SelectMany(b => b.Tags)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            var tagsList = new List<string> { "Tous" };
            tagsList.AddRange(uniqueTags);

            // Si le tag sélectionné n'existe plus, on repasse à "Tous"
            if (!tagsList.Contains(_selectedTag))
            {
                _selectedTag = "Tous";
            }

            foreach (var tag in tagsList)
            {
                bool isSelected = tag == _selectedTag;

                var border = new Border
                {
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(18) },
                    Stroke = Color.FromArgb("#512BD4"),
                    StrokeThickness = 1,
                    Padding = new Thickness(15, 6),
                    BackgroundColor = isSelected ? Color.FromArgb("#512BD4") : Colors.White,
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Center
                };

                var label = new Label
                {
                    Text = tag,
                    TextColor = isSelected ? Colors.White : Color.FromArgb("#512BD4"),
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center
                };

                border.Content = label;

                // Geste de clic pour appliquer le filtre
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) =>
                {
                    _selectedTag = tag;
                    UpdateFilterTags();
                    ApplyFilterAndSort();
                };
                border.GestureRecognizers.Add(tapGesture);

                TagsFilterLayout.Children.Add(border);
            }
        }

        private void ApplyFilterAndSort()
        {
            var filtered = _selectedTag == "Tous"
                ? _allBooks
                : _allBooks.Where(b => b.Tags != null && b.Tags.Contains(_selectedTag, StringComparer.OrdinalIgnoreCase)).ToList();

            var sortedList = _isAscending
                ? filtered.OrderBy(b => b.DateAdded).ToList()
                : filtered.OrderByDescending(b => b.DateAdded).ToList();

            // Mettre à jour la collection Books de manière à rafraîchir l'UI
            Books.Clear();
            foreach (var book in sortedList)
            {
                Books.Add(book);
            }
        }

        private async void OnImportClicked(object sender, EventArgs e)
        {
            try
            {
                // 1. Définition du filtre EPUB (Correction des namespaces)
                var epubFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>> {
                    { DevicePlatform.iOS, new[] { "org.idpf.epub-container" } },
                    { DevicePlatform.Android, new[] { "application/epub+zip" } },
                    { DevicePlatform.WinUI, new[] { ".epub" } },
                    { DevicePlatform.MacCatalyst, new[] { "org.idpf.epub-container" } }
                });

                // 2. Sélection du fichier
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Choisir un livre EPUB",
                    FileTypes = epubFileType
                });

                if (result != null)
                {
                    // Uploader via l'API (ceci copie également localement et extrait la couverture)
                    var newBook = await _bookService.UploadBookAsync(result);

                    _allBooks.Add(newBook);
                    UpdateFilterTags();
                    ApplyFilterAndSort();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Échec de l'import : {ex.Message}", "OK");
            }
        }

        private async void OnBookTapped(object sender, EventArgs e)
        {
            // 1. Récupérer l'élément cliqué (Frame ou VerticalStackLayout)
            var layout = (BindableObject)sender;
            var selectedBook = (Book)layout.BindingContext;

            if (selectedBook != null)
            {
                try
                {
                    // Télécharger le fichier ePub s'il n'est pas déjà dans le cache local
                    if (string.IsNullOrEmpty(selectedBook.FilePath) || !File.Exists(selectedBook.FilePath))
                    {
                        await _bookService.EnsureBookFileCachedAsync(selectedBook);
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Erreur de téléchargement", $"Impossible de télécharger le livre : {ex.Message}", "OK");
                    return;
                }

                // 2. Préparer le paramètre de navigation
                var navigationParameter = new Dictionary<string, object>
                {
                    { "SelectedBook", selectedBook }
                };

                // 3. Naviguer vers la page de détails avec l'objet Book
                await Shell.Current.GoToAsync("DetailsPage", navigationParameter);
            }
        }

        private async void OnDeleteBookClicked(object sender, EventArgs e)
        {
            // 1. Récupérer le bouton qui a été cliqué
            var button = (Button)sender;

            // 2. Récupérer l'objet "Book" lié à ce bouton via le CommandParameter
            var bookToDelete = (Book)button.CommandParameter;

            if (bookToDelete == null) return;

            // 3. Demander confirmation à l'utilisateur
            bool confirm = await DisplayAlert("Supprimer", $"Voulez-vous vraiment supprimer '{bookToDelete.Title}' ?", "Oui", "Non");

            if (confirm)
            {
                try
                {
                    // 4. Supprimer du serveur (et nettoyer localement)
                    await _bookService.DeleteBookAsync(bookToDelete.Id);

                    // 5. Retirer de la liste globale et mettre à jour
                    var bookInList = _allBooks.FirstOrDefault(b => b.Id == bookToDelete.Id);
                    if (bookInList != null)
                    {
                        _allBooks.Remove(bookInList);
                    }
                    UpdateFilterTags();
                    ApplyFilterAndSort();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Erreur", $"Impossible de supprimer le livre du serveur : {ex.Message}", "OK");
                }
            }
        }

        private void OnManageTagsClicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var book = (Book)button.CommandParameter;
            if (book == null) return;

            EditingBook = book;
            IsTagsPanelVisible = true;
            PopulateBookTags();
        }

        private void OnCloseTagsPanelClicked(object sender, EventArgs e)
        {
            IsTagsPanelVisible = false;
            EditingBook = null;
            UpdateFilterTags();
            ApplyFilterAndSort();
        }

        private void RefreshBookInList(Book book)
        {
            var visibleBook = Books.FirstOrDefault(b => b.Id == book.Id);
            if (visibleBook != null)
            {
                var index = Books.IndexOf(visibleBook);
                if (index >= 0)
                {
                    // Create a shallow copy of the Book to ensure the reference changes, forcing MAUI to rebind the item
                    var refreshedBook = new Book
                    {
                        Id = book.Id,
                        Title = book.Title,
                        FileName = book.FileName,
                        FilePath = book.FilePath,
                        CoverImagePath = book.CoverImagePath,
                        DateAdded = book.DateAdded,
                        LastPageRead = book.LastPageRead,
                        Tags = book.Tags
                    };

                    // Also update the book reference in our main list _allBooks
                    var allBooksIndex = _allBooks.FindIndex(b => b.Id == book.Id);
                    if (allBooksIndex >= 0)
                    {
                        _allBooks[allBooksIndex] = refreshedBook;
                    }
                    if (EditingBook?.Id == book.Id)
                    {
                        EditingBook = refreshedBook;
                    }

                    Books[index] = refreshedBook;
                }
            }
        }

        private async void OnAddTagClicked(object sender, EventArgs e)
        {
            var newTag = NewTagEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(newTag)) return;

            if (EditingBook != null)
            {
                await _bookService.AddTagToBookAsync(EditingBook.Id, newTag);
                if (EditingBook.Tags == null)
                {
                    EditingBook.Tags = new List<string>();
                }
                if (!EditingBook.Tags.Contains(newTag, StringComparer.OrdinalIgnoreCase))
                {
                    EditingBook.Tags.Add(newTag);
                }
                NewTagEntry.Text = string.Empty;
                RefreshBookInList(EditingBook);
                PopulateBookTags();
            }
        }

        private async void PopulateBookTags()
        {
            BookTagsFlexLayout.Children.Clear();
            AllTagsFlexLayout.Children.Clear();
            
            if (EditingBook == null) return;

            // 1. Populate current book's tags
            if (EditingBook.Tags != null)
            {
                foreach (var tag in EditingBook.Tags.ToList())
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
                        await _bookService.RemoveTagFromBookAsync(EditingBook.Id, tag);
                        EditingBook.Tags.Remove(tag);
                        RefreshBookInList(EditingBook);
                        PopulateBookTags();
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
                var currentBookTags = EditingBook.Tags ?? new List<string>();
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
                        await _bookService.AddTagToBookAsync(EditingBook.Id, tag);
                        if (EditingBook.Tags == null)
                        {
                            EditingBook.Tags = new List<string>();
                        }
                        if (!EditingBook.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                        {
                            EditingBook.Tags.Add(tag);
                        }
                        RefreshBookInList(EditingBook);
                        PopulateBookTags();
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
    }
}