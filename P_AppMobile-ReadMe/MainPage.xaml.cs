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
            var savedBooks = await _bookService.LoadBooksAsync();
            foreach (var book in savedBooks)
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
                    // 3. Extraction des métadonnées avec VersOne.Epub
                    // On ouvre le flux une seule fois pour l'analyse
                    using var stream = await result.OpenReadAsync();
                    var epubBook = await EpubReader.ReadBookAsync(stream);
                    string title = epubBook.Title ?? result.FileName;

                    // 4. Copie locale du fichier dans le dossier de l'application
                    // On utilise FileSystem.AppDataDirectory comme dans le JsonDataService
                    string localFolder = FileSystem.AppDataDirectory;
                    string targetPath = Path.Combine(localFolder, result.FileName);

                    // Copie physique du fichier pour qu'il reste accessible plus tard
                    using (var sourceStream = await result.OpenReadAsync())
                    using (var targetStream = File.Create(targetPath))
                    {
                        await sourceStream.CopyToAsync(targetStream);
                    }

                    // 5. Extraction et sauvegarde de l'image de couverture
                    string coverImagePath = string.Empty;
                    if (epubBook.CoverImage != null)
                    {
                        string coverFileName = Path.GetFileNameWithoutExtension(result.FileName) + "_cover.jpg";
                        coverImagePath = Path.Combine(localFolder, coverFileName);
                        await File.WriteAllBytesAsync(coverImagePath, epubBook.CoverImage);
                    }

                    // 6. Création de l'objet et mise à jour de la liste
                    var newBook = new Book
                    {
                        Title = title,
                        FileName = result.FileName,
                        FilePath = targetPath,
                        CoverImagePath = coverImagePath,
                        DateAdded = DateTime.Now
                    };

                    Books.Add(newBook);

                    // Sauvegarde persistante (nécessite System.Linq pour .ToList())
                    await _bookService.SaveBooksAsync(Books.ToList());
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

                // 4. Retirer de la liste affichée
                Books.Remove(bookToDelete);

                // 5. Sauvegarder la nouvelle liste dans le fichier JSON
                await _bookService.SaveBooksAsync(Books.ToList());
            }
        }
    }
}