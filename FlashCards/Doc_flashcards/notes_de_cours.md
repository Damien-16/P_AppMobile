# Glossaire des Notions Mobiles (ICT-335)

Ce glossaire regroupe de manière simple les définitions et termes clés vus en classe et mis en pratique dans l'application de flashcards.

---

### Navigation & Interface (UI)

*   **Shell** : La structure globale de l'application qui gère la barre de navigation, les onglets et la navigation générale.
*   **Routing** : Système de navigation par URI (adresses textuelles). On déclare une page dans `AppShell.xaml.cs` (ex: `Routing.RegisterRoute("PageCartes", typeof(CardsPage))`) et on y navigue avec `Shell.Current.GoToAsync("PageCartes")`.
*   **QueryProperty** : Attribut placé en haut d'une classe de page pour récupérer des données envoyées depuis la page précédente lors de la navigation (ex: récupérer le deck choisi).
*   **Grid** : Conteneur en forme de tableau (lignes et colonnes) pour placer précisément les boutons et textes sur l'écran.
*   **StackLayout** : Conteneur simple qui empile ses éléments enfants les uns sur les autres (verticalement) ou côte à côte (horizontalement).
*   **Code-behind** : Fichier C# attaché à une vue XAML (ex: `MainPage.xaml.cs`) qui contient le code logique réagissant aux clics ou autres actions de l'utilisateur.

---

### Données & Persistance

*   **CRUD** : Acronyme de *Create, Read, Update, Delete* (Créer, Lire, Modifier, Supprimer). Il s'agit des quatre opérations de base pour gérer les éléments (decks et cartes) dans notre application.
*   **DataBinding** : Mécanisme reliant automatiquement une propriété graphique (ex: le texte d'un Label) à une propriété C# dans le code.
*   **CollectionView** : Composant de liste performant servant à afficher des listes d'éléments défilables (comme nos decks).
*   **INotifyPropertyChanged** : Interface C# obligatoire à implémenter sur nos modèles/classes de données pour indiquer à l'écran qu'une information a changé et qu'il faut rafraîchir l'affichage visuel.
*   **AppDataDirectory** : Emplacement mémoire sécurisé et privé sur le smartphone où l'application peut écrire des fichiers (utilisé pour stocker notre fichier JSON).
*   **JSON Serialization** : Action de convertir nos objets C# (listes, decks, cartes) en texte structuré JSON afin de pouvoir les sauvegarder de manière persistante sur le téléphone, puis les recharger au démarrage.

---

### Animations & Capteurs (Interaction)

*   **Animations MAUI** : Méthodes de transition asynchrones intégrées pour modifier visuellement un élément : `TranslateTo` (déplacement), `FadeTo` (opacité), et `ScaleTo` (taille).
*   **Flip Animation** : Animation simulant le retournement d'une carte en réduisant sa largeur à zéro (`ScaleXTo(0)`) puis en la ré-agrandissant (`ScaleXTo(1)`) après avoir changé son texte.
*   **Accelerometer (Accéléromètre)** : Capteur matériel interne détectant l'inclinaison et les secousses du smartphone. L'événement `ShakeDetected` permet de déclencher une action (marquer la carte comme fausse) dès que l'utilisateur secoue son téléphone.
