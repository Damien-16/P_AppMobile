using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P_AppMobile_ReadMe.Models
{
    public class Book
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime DateAdded { get; set; } = DateTime.Now;

        public string DisplayDate => $"Ajouté le: {DateAdded:dd.MM.yyyy}";
    }
}
