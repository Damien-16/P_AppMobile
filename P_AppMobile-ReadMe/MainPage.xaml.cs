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
        // Déclaration du service et de la liste pour l'UI
        private readonly BookService _bookService;
        public ObservableCollection<Book> Books { get; set; } = new ObservableCollection<Book>();
        private bool _isAscending = false; // Par défaut, les plus récents en premier

        public MainPage()
        {
            InitializeComponent();

            // Initialisation du service (basé sur la logique du projet FlashCards)
            _bookService = new BookService();

            // Indispensable pour que le XAML puisse voir la liste "Books"
            BindingContext = this;

            // Charger les livres existants au démarrage
            LoadSavedBooks();
        }

        private async void LoadSavedBooks()
        {
            try
            {
                var savedBooks = await _bookService.LoadBooksAsync();
                foreach (var book in savedBooks)
                {
                    Books.Add(book);
                }
                ApplySort();

                // Téléchargement et extraction en arrière-plan des couvertures/fichiers manquants
                _ = Task.Run(async () =>
                {
                    foreach (var book in savedBooks.ToList())
                    {
                        if (string.IsNullOrEmpty(book.CoverImagePath) || !File.Exists(book.CoverImagePath))
                        {
                            try
                            {
                                await _bookService.EnsureBookFileCachedAsync(book);
                                // Mettre à jour l'UI sur le thread principal
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    var index = Books.IndexOf(book);
                                    if (index >= 0)
                                    {
                                        Books[index] = book;
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
            ApplySort();
        }

        private void ApplySort()
        {
            if (Books.Count <= 1) return;

            var sortedList = _isAscending
                ? Books.OrderBy(b => b.DateAdded).ToList()
                : Books.OrderByDescending(b => b.DateAdded).ToList();

            // Vider et re-remplir pour notifier l'UI
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

                    Books.Add(newBook);
                    ApplySort(); // Maintenir le tri après ajout
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

                    // 5. Retirer de la liste affichée
                    Books.Remove(bookToDelete);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Erreur", $"Impossible de supprimer le livre du serveur : {ex.Message}", "OK");
                }
            }
        }
    }
}