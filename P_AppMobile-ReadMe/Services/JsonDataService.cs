using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using VersOne.Epub;
using P_AppMobile_ReadMe.Models;

namespace P_AppMobile_ReadMe.Services
{
    public class ApiBook
    {
        public int id { get; set; }
        public string title { get; set; } = string.Empty;
        public string author { get; set; } = string.Empty;
        public string? description { get; set; }
        public string epub_file_path { get; set; } = string.Empty;
        public string? cover_image_path { get; set; }
        public int file_size_bytes { get; set; }
        public string uploaded_at { get; set; } = string.Empty;
    }

    public class BookMetadata
    {
        public string Id { get; set; } = string.Empty;
        public int LastPageRead { get; set; } = 0;
        public List<string> Tags { get; set; } = new List<string>();
        public string LocalFilePath { get; set; } = string.Empty;
        public string LocalCoverImagePath { get; set; } = string.Empty;
    }

    public class BookService
    {
        private readonly HttpClient _httpClient;
        private readonly string _metadataFilePath;
        private const string ApiBaseUrl = "http://localhost:3000";

        public BookService()
        {
            _httpClient = new HttpClient();
            _metadataFilePath = Path.Combine(FileSystem.AppDataDirectory, "metadata.json");
        }

        public async Task<List<Book>> LoadBooksAsync()
        {
            try
            {
                // 1. Fetch book list from the API
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}/books");
                if (!response.IsSuccessStatusCode) return await LoadLocalCachedBooksOnly();

                var json = await response.Content.ReadAsStringAsync();
                var apiBooks = JsonSerializer.Deserialize<List<ApiBook>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ApiBook>();

                // 2. Load local metadata
                var metadataList = await LoadMetadataAsync();

                var books = new List<Book>();
                foreach (var apiBook in apiBooks)
                {
                    var idStr = apiBook.id.ToString();
                    var meta = metadataList.FirstOrDefault(m => m.Id == idStr);
                    if (meta == null)
                    {
                        meta = new BookMetadata { Id = idStr };
                        metadataList.Add(meta);
                    }

                    var book = new Book
                    {
                        Id = idStr,
                        Title = apiBook.title,
                        FileName = Path.GetFileName(apiBook.epub_file_path),
                        FilePath = meta.LocalFilePath,
                        CoverImagePath = meta.LocalCoverImagePath,
                        DateAdded = DateTime.TryParse(apiBook.uploaded_at, out var dt) ? dt : DateTime.Now,
                        LastPageRead = meta.LastPageRead,
                        Tags = meta.Tags
                    };
                    books.Add(book);
                }

                await SaveMetadataAsync(metadataList);
                return books;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading books from API: {ex.Message}");
                return await LoadLocalCachedBooksOnly();
            }
        }

        public async Task SaveBooksAsync(List<Book> books)
        {
            var metadataList = await LoadMetadataAsync();
            foreach (var book in books)
            {
                var meta = metadataList.FirstOrDefault(m => m.Id == book.Id);
                if (meta == null)
                {
                    meta = new BookMetadata { Id = book.Id };
                    metadataList.Add(meta);
                }
                meta.LastPageRead = book.LastPageRead;
                meta.Tags = book.Tags;
                meta.LocalFilePath = book.FilePath;
                meta.LocalCoverImagePath = book.CoverImagePath;
            }
            await SaveMetadataAsync(metadataList);
        }

        public async Task<List<string>> GetAllTagsAsync()
        {
            var books = await LoadBooksAsync();
            return books
                .Where(b => b.Tags != null)
                .SelectMany(b => b.Tags)
                .Distinct()
                .OrderBy(t => t)
                .ToList();
        }

        public async Task AddTagToBookAsync(string bookId, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;
            tag = tag.Trim();

            var books = await LoadBooksAsync();
            var book = books.FirstOrDefault(b => b.Id == bookId);
            if (book != null)
            {
                if (book.Tags == null)
                {
                    book.Tags = new List<string>();
                }

                if (!book.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                {
                    book.Tags.Add(tag);
                    await SaveBooksAsync(books);
                }
            }
        }

        public async Task RemoveTagFromBookAsync(string bookId, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;
            tag = tag.Trim();

            var books = await LoadBooksAsync();
            var book = books.FirstOrDefault(b => b.Id == bookId);
            if (book != null && book.Tags != null)
            {
                var existingTag = book.Tags.FirstOrDefault(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));
                if (existingTag != null)
                {
                    book.Tags.Remove(existingTag);
                    await SaveBooksAsync(books);
                }
            }
        }

        public async Task EnsureBookFileCachedAsync(Book book)
        {
            if (string.IsNullOrEmpty(book.Id)) return;

            if (!string.IsNullOrEmpty(book.FilePath) && File.Exists(book.FilePath))
            {
                return;
            }

            try
            {
                var downloadUrl = $"{ApiBaseUrl}/book/{book.Id}/file";
                var response = await _httpClient.GetAsync(downloadUrl);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to download epub file. Status: {response.StatusCode}");
                }

                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                var localFolder = FileSystem.AppDataDirectory;
                var localFilePath = Path.Combine(localFolder, $"{book.Id}_{book.FileName}");
                await File.WriteAllBytesAsync(localFilePath, fileBytes);
                book.FilePath = localFilePath;

                var coverImagePath = string.Empty;
                using (var stream = new MemoryStream(fileBytes))
                {
                    var epubBook = await EpubReader.ReadBookAsync(stream);
                    if (epubBook.CoverImage != null)
                    {
                        var coverFileName = $"{book.Id}_cover.jpg";
                        coverImagePath = Path.Combine(localFolder, coverFileName);
                        await File.WriteAllBytesAsync(coverImagePath, epubBook.CoverImage);
                    }
                }
                book.CoverImagePath = coverImagePath;

                var metadataList = await LoadMetadataAsync();
                var meta = metadataList.FirstOrDefault(m => m.Id == book.Id);
                if (meta == null)
                {
                    meta = new BookMetadata { Id = book.Id };
                    metadataList.Add(meta);
                }
                meta.LocalFilePath = localFilePath;
                meta.LocalCoverImagePath = coverImagePath;
                await SaveMetadataAsync(metadataList);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error caching book {book.Id}: {ex.Message}");
                throw;
            }
        }

        public async Task<Book> UploadBookAsync(FileResult fileResult)
        {
            using var stream = await fileResult.OpenReadAsync();
            using var content = new MultipartFormDataContent();
            
            // Read stream into byte array for HTTP transmission
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            var streamContent = new ByteArrayContent(fileBytes);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/epub+zip");
            content.Add(streamContent, "file", fileResult.FileName);

            var uploadUrl = $"{ApiBaseUrl}/books/upload";
            var response = await _httpClient.PostAsync(uploadUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to upload epub file: {response.StatusCode} - {errorText}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var apiBook = JsonSerializer.Deserialize<ApiBook>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (apiBook == null)
            {
                throw new Exception("Failed to parse API upload response.");
            }

            var localFolder = FileSystem.AppDataDirectory;
            var localFilePath = Path.Combine(localFolder, $"{apiBook.id}_{fileResult.FileName}");
            await File.WriteAllBytesAsync(localFilePath, fileBytes);

            string coverImagePath = string.Empty;
            using (var coverStream = new MemoryStream(fileBytes))
            {
                var epubBook = await EpubReader.ReadBookAsync(coverStream);
                if (epubBook.CoverImage != null)
                {
                    var coverFileName = $"{apiBook.id}_cover.jpg";
                    coverImagePath = Path.Combine(localFolder, coverFileName);
                    await File.WriteAllBytesAsync(coverImagePath, epubBook.CoverImage);
                }
            }

            var newBook = new Book
            {
                Id = apiBook.id.ToString(),
                Title = apiBook.title,
                FileName = fileResult.FileName,
                FilePath = localFilePath,
                CoverImagePath = coverImagePath,
                DateAdded = DateTime.TryParse(apiBook.uploaded_at, out var dt) ? dt : DateTime.Now,
                LastPageRead = 0,
                Tags = new List<string>()
            };

            var metadataList = await LoadMetadataAsync();
            metadataList.Add(new BookMetadata
            {
                Id = newBook.Id,
                LocalFilePath = localFilePath,
                LocalCoverImagePath = coverImagePath,
                LastPageRead = 0,
                Tags = new List<string>()
            });
            await SaveMetadataAsync(metadataList);

            return newBook;
        }

        public async Task DeleteBookAsync(string bookId)
        {
            if (string.IsNullOrEmpty(bookId)) return;

            var deleteUrl = $"{ApiBaseUrl}/book/{bookId}/delete";
            var response = await _httpClient.DeleteAsync(deleteUrl);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to delete book from server. Status: {response.StatusCode}");
            }

            var metadataList = await LoadMetadataAsync();
            var meta = metadataList.FirstOrDefault(m => m.Id == bookId);
            if (meta != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(meta.LocalFilePath) && File.Exists(meta.LocalFilePath))
                    {
                        File.Delete(meta.LocalFilePath);
                    }
                    if (!string.IsNullOrEmpty(meta.LocalCoverImagePath) && File.Exists(meta.LocalCoverImagePath))
                    {
                        File.Delete(meta.LocalCoverImagePath);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete cache files: {ex.Message}");
                }

                metadataList.Remove(meta);
                await SaveMetadataAsync(metadataList);
            }
        }

        private async Task<List<Book>> LoadLocalCachedBooksOnly()
        {
            var metadataList = await LoadMetadataAsync();
            var books = new List<Book>();
            foreach (var meta in metadataList)
            {
                if (string.IsNullOrEmpty(meta.LocalFilePath) || !File.Exists(meta.LocalFilePath))
                    continue;

                var book = new Book
                {
                    Id = meta.Id,
                    Title = Path.GetFileNameWithoutExtension(meta.LocalFilePath),
                    FileName = Path.GetFileName(meta.LocalFilePath),
                    FilePath = meta.LocalFilePath,
                    CoverImagePath = meta.LocalCoverImagePath,
                    DateAdded = File.GetCreationTime(meta.LocalFilePath),
                    LastPageRead = meta.LastPageRead,
                    Tags = meta.Tags
                };
                books.Add(book);
            }
            return books;
        }

        private async Task<List<BookMetadata>> LoadMetadataAsync()
        {
            try
            {
                if (!File.Exists(_metadataFilePath)) return new List<BookMetadata>();
                string json = await File.ReadAllTextAsync(_metadataFilePath);
                return JsonSerializer.Deserialize<List<BookMetadata>>(json) ?? new List<BookMetadata>();
            }
            catch { return new List<BookMetadata>(); }
        }

        private async Task SaveMetadataAsync(List<BookMetadata> metadata)
        {
            try
            {
                var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_metadataFilePath, json);
            }
            catch { }
        }
    }
}
