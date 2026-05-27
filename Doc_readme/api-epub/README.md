## 🛣️ API Endpoints

The API limits file uploads to 50MB.

| Method   | Endpoint            | Description                           |
|:---------|:--------------------|:--------------------------------------|
| `GET`    | `/`                 | Welcome message                       |
| `GET`    | `/books`            | List all available books              |
| `POST`   | `/books/upload`     | Upload a new EPUB book                |
| `GET`    | `/book/{id}`        | Get specific book metadata (JSON)     |
| `GET`    | `/book/{id}/file`   | Download the raw EPUB file            |
| `DELETE` | `/book/{id}/delete` | Delete a book and its associated file |

## 🧪 Database Schema

The `Books` table includes the following fields:
- `Id`: Primary Key (Auto-increment)
- `Title`: Book title
- `Author`: Author name
- `Description`: Optional synopsis
- `EpubFilePath`: Storage path to the .epub file (S3 key or local relative path)
- `FileSizeBytes`: Size of the file
- `UploadedAt`: Timestamp of upload
