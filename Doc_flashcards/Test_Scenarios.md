# Scénarios de Test - Application FlashCards

## Scénario 1 : Boucle d'apprentissage jusqu'à connu
**Objectif :** Vérifier que les cartes marquées comme "Faux" reviennent en fin de pile jusqu'à ce qu'elles soient validées.

1. **Action :** Lancer un deck de 3 cartes.
2. **Action :** Sur la 1ere carte, cliquer sur Faux.
3. **Résultat Attendu :** La 2eme carte s'affiche.
4. **Action :** Valider les 2eme et 3eme carte.
5. **Résultat Attendu :** La 1ere carte réapparaît après la 3eme carte.
6. **Action :** Cliquer sur Correct pour la 1ere carte.
7. **Résultat Attendu :** La session se termine car toutes les cartes ont été validées au moins une fois.

## Scénario 2 : Gestion Complète des Deck et Cartes
**Objectif :** Vérifier la création, modification et suppression d'un deck et de ses cartes.

1. **Action :** Créer un nouveau deck nommé "Test".
2. **Action :** Ajouter une carte avec Recto: "Recto" et Verso: "Verso".
3. **Résultat Attendu :** Dans la page des decks, le deck apparaît dans la liste et affiche "1 carte".
4. **Action :** Modifier la carte pour changer le Verso en "Verso Modifié".
5. **Action :** Supprimer la carte du deck.
6. **Résultat Attendu :** Le deck affiche "0 carte".
7. **Action :** Supprimer le deck "Test".
8. **Résultat Attendu :** Le deck ne figure plus dans la liste principale.

## Scénario 3 : Détection de Secousse (Shake to Fail)
**Objectif :** Vérifier que secouer l'appareil marque la carte actuelle comme "Incorrecte".

1. **Action :** Lancer une session d'étude.
2. **Action :** Secouer l'appareil (ou simuler via l'émulateur).
3. **Résultat Attendu :** La carte subit l'animation de sortie vers la gauche (animation "Faux").
4. **Résultat Attendu :** La carte est ajoutée à la fin de la pile (vérifiable en attendant la fin du cycle).
