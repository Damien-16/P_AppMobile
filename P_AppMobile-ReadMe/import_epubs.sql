-- 1. Création de la table avec un champ EpubContent de type BLOB
CREATE TABLE IF NOT EXISTS Books_Blob (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT,
    Author TEXT,
    Description TEXT,
    EpubContent BLOB,
    UploadedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Remplacer les chemins par les bons chemins absolus ou relatifs selon l'environnement d'exécution
INSERT INTO Books_Blob (Title, Author, EpubContent)
VALUES (
    'Fables',
    'Jean de La Fontaine',
    readfile('Doc_readme/api-epub/data/books/La Fontaine, Jean de - Fables.epub')
);

