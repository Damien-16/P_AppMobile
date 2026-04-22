using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using P_AppMobile_ReadMe.Models;

namespace P_AppMobile_ReadMe.Services
{
    public class BookService
    {
        private readonly string _filePath;

        public BookService()
        {
            _filePath = Path.Combine(FileSystem.AppDataDirectory, "books.json");
        }

        public async Task<List<Book>> LoadBooksAsync()
        {
            try
            {
                if (!File.Exists(_filePath)) return new List<Book>();
                string json = await File.ReadAllTextAsync(_filePath);
                return JsonSerializer.Deserialize<List<Book>>(json) ?? new List<Book>();
            }
            catch { return new List<Book>(); }
        }

        public async Task SaveBooksAsync(List<Book> books)
        {
            var json = JsonSerializer.Serialize(books, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json);
        }
    }
}
