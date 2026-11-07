# Musique pour l'auto

Petit utilitaire Windows, très simple, qui transforme une vidéo YouTube d'une chanson en **fichier MP3 prêt pour une clé USB**, lisible dans la radio d'une automobile (Toyota Tacoma, Jeep Cherokee et la plupart des autres).

![Icône](src/icone.png)

- On colle le lien YouTube, puis on clique sur un gros bouton vert.
- La chanson est enregistrée dans le dossier **Musique › Musique pour l'auto**.
- Le bouton **« Copier sur la clé USB »** copie les nouvelles chansons sur la clé.

Tout est en français, sans fenêtre noire, sans réglage et sans compte à créer.

---

## Installation (pour la personne qui installe)

1. Téléchargez **`MusiquePourAuto.exe`** depuis la page [Releases](../../releases/latest).
2. Placez-le sur le Bureau de l'ordinateur (ou dans n'importe quel dossier).
3. Double-cliquez dessus.
   - Si Windows affiche **« Windows a protégé votre ordinateur »**, cliquez sur **« Informations complémentaires »**, puis sur **« Exécuter quand même »**. Cela n'arrive qu'à la première ouverture : l'application est gratuite et n'est pas signée par un éditeur payant.
   - Si Windows **bloque l'application sans proposer « Exécuter quand même »**, c'est le **Contrôle intelligent des applications** de Windows 11 (voir [Dépannage](#dépannage)).
4. **Première ouverture seulement** : l'application télécharge ses outils (environ 160 Mo : yt-dlp, ffmpeg et deno). Comptez de quelques secondes à quelques minutes selon la connexion. Ensuite, tout est prêt.

Configuration requise : Windows 10 ou 11 (64 bits) et une connexion Internet. Rien d'autre à installer : l'application utilise le .NET Framework déjà inclus dans Windows.

> Astuce : clic droit sur `MusiquePourAuto.exe` › **Afficher d'autres options** › **Envoyer vers** › **Bureau (créer un raccourci)** si vous préférez garder le fichier ailleurs.

---

## Mode d'emploi (pour la personne qui écoute la musique)

1. Ouvrez YouTube et trouvez la chanson.
2. Sous la vidéo, cliquez sur **« Partager »**, puis sur **« Copier »**.
3. Ouvrez **Musique pour l'auto** et cliquez sur **« Coller le lien »**.
   (Si vous venez de copier un lien YouTube, il est souvent déjà collé automatiquement.)
4. Cliquez sur le gros bouton vert **« Télécharger la chanson en MP3 »**. Attendez le message vert **« C'est prêt ! »**.
5. Recommencez pour chaque chanson voulue.
6. Branchez la clé USB dans l'ordinateur et cliquez sur **« Copier sur la clé USB »**.
7. Quand le message dit que c'est terminé, retirez la clé et branchez-la dans l'auto. Sur la radio, choisissez la source **USB** (ou **Média**).

Dans la liste **« Vos chansons »** :
- **double-clic** sur une chanson pour l'écouter sur l'ordinateur;
- **clic droit › Supprimer** pour l'envoyer à la corbeille.

Le lien **Aide**, en bas de la fenêtre, rappelle ces étapes.

---

## La clé USB et l'auto

- La clé doit être en format **FAT32**. C'est le format le plus compatible avec les radios d'auto, y compris Toyota (Entune / Audio Multimedia) et Jeep (Uconnect).
- **Conseil : utilisez une clé de 32 Go ou moins.** Elles sont presque toujours vendues en FAT32, et Windows peut les formater en FAT32 lui-même. Pour une clé de plus de 32 Go, Windows ne propose que exFAT ou NTFS, que certaines radios ne lisent pas.
- Si l'application affiche un avertissement sur le format de la clé, la clé peut quand même fonctionner. Si l'auto ne la lit pas :
  - **Attention : formater efface tout le contenu de la clé.**
  - Dans l'Explorateur de fichiers : clic droit sur la clé › **Formater…** › Système de fichiers **FAT32** › **Démarrer**.
- Les chansons sont copiées dans un dossier **`Musique`** sur la clé. Au-delà de 250 chansons, l'application crée `Musique 2`, `Musique 3`, etc. (certaines radios Toyota limitent le nombre de fichiers par dossier).

### Format des fichiers produits

Les MP3 sont réglés pour éviter les problèmes connus des radios d'auto :

| Réglage | Valeur | Pourquoi |
|---|---|---|
| Codec | MP3 (MPEG-1 Layer III) | Lu par toutes les radios |
| Débit | **192 kb/s constant (CBR)** | Le débit variable fausse la durée affichée ou fait sauter certaines radios |
| Fréquence | **44 100 Hz**, stéréo | YouTube fournit souvent du 48 000 Hz, que certaines radios refusent |
| Étiquettes | **ID3v2.3** + ID3v1 (titre et artiste) | Beaucoup de radios ne lisent pas l'ID3v2.4 (format par défaut de ffmpeg) |
| Image intégrée | **aucune** | Les grosses pochettes intégrées peuvent bloquer la lecture sur certaines radios |
| Nom de fichier | `Artiste - Titre.mp3`, 100 caractères max. | Caractères interdits en FAT32 retirés, émojis retirés, accents conservés |

L'application nettoie aussi les titres YouTube : `Queen – Bohemian Rhapsody (Official Video Remastered)` devient `Queen - Bohemian Rhapsody.mp3`. Les mentions utiles comme `(Live)`, `(Remix)` ou `(feat. …)` sont conservées.

Si le lien vient d'une liste de lecture (`&list=…`), seule la vidéo choisie est téléchargée.

---

## Où sont les fichiers ?

| Quoi | Où |
|---|---|
| Les chansons | `Musique\Musique pour l'auto` (bouton « Ouvrir le dossier des chansons ») |
| Les outils (yt-dlp, ffmpeg, deno) | `%LOCALAPPDATA%\MusiquePourAuto\outils` |
| Le journal (pour le dépannage) | `%LOCALAPPDATA%\MusiquePourAuto\journal.txt` |

yt-dlp se met à jour automatiquement (au plus une fois par jour, au démarrage) : YouTube change souvent et une vieille version cesse de fonctionner. Si un téléchargement échoue, l'application fait aussi une mise à jour, puis un nouvel essai.

---

## Dépannage

| Message | Que faire |
|---|---|
| « Ce texte n'est pas un lien » | Recopiez le lien avec **Partager › Copier** dans YouTube. |
| « Cette vidéo n'est pas disponible » / « est privée » / « réservée aux adultes » | Cherchez une autre vidéo de la même chanson. |
| « Impossible de joindre YouTube » | Vérifiez la connexion Internet. |
| « YouTube bloque temporairement… » | Attendez une heure environ, puis réessayez. |
| « Aucune clé USB trouvée » | Branchez la clé, attendez quelques secondes, puis recliquez. |
| L'auto ne lit pas la clé | Vérifiez le format FAT32 (voir plus haut). |
| Windows bloque l'application **sans** bouton « Exécuter quand même » (Windows 11) | C'est le **Contrôle intelligent des applications**, qui bloque tout programme non signé (l'application et ses outils). Pour l'utiliser : **Sécurité Windows › Contrôle des applications et du navigateur › Paramètres du contrôle intelligent des applications › Désactivé**. Attention : cela réduit une protection de Windows et, sur certaines versions de Windows 11, elle ne peut plus être réactivée ensuite. |
| La préparation (premier lancement) échoue | Vérifiez la connexion Internet, puis cliquez sur « Réessayer la préparation ». Un antivirus peut aussi bloquer les outils téléchargés : consultez `journal.txt`. |
| Autre problème | Envoyez le fichier `journal.txt` (lien **Aide** › chemin indiqué) à la personne qui a installé l'application. |

Pour **réinstaller les outils**, fermez l'application, supprimez le dossier `%LOCALAPPDATA%\MusiquePourAuto\outils`, puis rouvrez l'application.

### Désinstaller

Supprimez `MusiquePourAuto.exe` et le dossier `%LOCALAPPDATA%\MusiquePourAuto`. Les chansons restent dans `Musique\Musique pour l'auto`.

---

## Pour les développeurs

Le code est en C# 5 / WinForms, compilé avec le compilateur `csc.exe` du .NET Framework 4.x **inclus dans Windows**. Aucun SDK n'est requis.

```bat
build.cmd    :: produit build\MusiquePourAuto.exe
test.cmd     :: compile et lance les tests du nettoyage des titres
```

| Fichier | Rôle |
|---|---|
| `src/FenetrePrincipale.cs` | Interface (3 étapes, gros boutons, messages simples) |
| `src/Conversion.cs` | yt-dlp (audio seulement) puis ffmpeg (MP3 CBR 192k, 44,1 kHz, ID3v2.3) |
| `src/Outils.cs` | Emplacements, journal, lancement de processus, installation et mise à jour des outils |
| `src/NomsChansons.cs` | Déduction artiste/titre, nettoyage des titres, noms de fichiers compatibles FAT32 |
| `src/CleUsb.cs` | Détection des clés USB, vérification du format, copie |
| `src/Programme.cs` | Point d'entrée, instance unique, mode `--auto-test` |
| `scripts/creer-icone.ps1` | Régénère `src/icone.ico` |

Test de bout en bout sans interface (installe les outils si besoin, télécharge, convertit) :

```bat
set MUSIQUEAUTO_HOME=%TEMP%\ma-test
set MUSIQUEAUTO_MUSIQUE=%TEMP%\ma-test\chansons
build\MusiquePourAuto.exe --auto-test "https://www.youtube.com/watch?v=jNQXAC9IVRw"
type %TEMP%\ma-test\auto-test.txt
```

Outils téléchargés au premier lancement :
- [yt-dlp](https://github.com/yt-dlp/yt-dlp) : `releases/latest/download/yt-dlp.exe`
- [FFmpeg (builds de yt-dlp)](https://github.com/yt-dlp/FFmpeg-Builds) : `ffmpeg-master-latest-win64-gpl-shared.zip`
- [Deno](https://github.com/denoland/deno) : moteur JavaScript exigé par yt-dlp pour YouTube

---

## Utilisation responsable

Cet outil est destiné à un usage personnel. Téléchargez uniquement du contenu que vous avez le droit de copier, en respectant les droits d'auteur et les conditions d'utilisation de YouTube.

Licence : MIT (voir `LICENSE`).
