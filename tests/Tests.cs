// Petits tests sans dépendance pour NomsChansons. Lancer : test.cmd
using System;
using System.IO;
using MusiquePourAuto;

static class Tests
{
    static int echecs = 0, total = 0;

    static void Egal(string attendu, string obtenu, string cas)
    {
        total++;
        if (attendu == obtenu) return;
        echecs++;
        Console.WriteLine("ECHEC  {0}\n   attendu : [{1}]\n   obtenu  : [{2}]", cas, attendu, obtenu);
    }

    static void Vrai(bool valeur, string cas)
    {
        Egal("True", valeur.ToString(), cas);
    }

    static void Erreur(string sortieYtDlp, string extraitAttendu, bool miseAJourAttendue)
    {
        bool miseAJour;
        string message = Conversion.ExpliquerErreur(sortieYtDlp, out miseAJour);
        Vrai(message.Contains(extraitAttendu), "message d'erreur pour : " + sortieYtDlp + " -> " + message);
        Egal(miseAJourAttendue.ToString(), miseAJour.ToString(), "mise à jour utile pour : " + sortieYtDlp);
    }

    static void TesterEmpreintes()
    {
        const string a = "f5389d3698ecde4ef06f4e6b1ce0294fe9ac1396228c8990b8d3416d6d177a7d";
        const string b = "036988a43f3171607cd7434960b3ce3ef04d30f0d29ec525ba3fe8c34653e9ac";
        const string c = "1111111111111111111111111111111111111111111111111111111111111111";

        // Format sha256sum de yt-dlp : plusieurs fichiers aux noms proches.
        string ytdlp = c + "  yt-dlp\n" + a + "  yt-dlp.exe\n" + b + "  yt-dlp_x86.exe\n" + c + "  yt-dlp_win.zip\n";
        Egal(a, Outils.TrouverEmpreinte(ytdlp, "yt-dlp.exe"), "empreinte yt-dlp.exe parmi des noms proches");
        Egal(b, Outils.TrouverEmpreinte(ytdlp, "yt-dlp_x86.exe"), "empreinte yt-dlp_x86.exe");

        // Format FFmpeg-Builds : « gpl » et « gpl-shared » ne doivent pas être confondus.
        string ffmpeg = a + "  ffmpeg-master-latest-win64-gpl.zip\r\n" + b + "  ffmpeg-master-latest-win64-gpl-shared.zip\r\n";
        Egal(b, Outils.TrouverEmpreinte(ffmpeg, "ffmpeg-master-latest-win64-gpl-shared.zip"), "empreinte ffmpeg gpl-shared (fins de ligne Windows)");
        Egal(a, Outils.TrouverEmpreinte(ffmpeg, "ffmpeg-master-latest-win64-gpl.zip"), "empreinte ffmpeg gpl");

        // Marqueur binaire « *nom » de sha256sum.
        Egal(a, Outils.TrouverEmpreinte(a + " *outil.zip\n", "outil.zip"), "marqueur binaire *");

        // Format Get-FileHash de PowerShell (Deno) : empreinte en majuscules, nom sur une autre ligne.
        string deno = "\r\nAlgorithm : SHA256\r\nHash      : " + b.ToUpperInvariant() + "\r\nPath      : C:\\a\\deno\\deno\\target\\release\\deno-x86_64-pc-windows-msvc.zip\r\n\r\n";
        Egal(b, Outils.TrouverEmpreinte(deno, "deno-x86_64-pc-windows-msvc.zip"), "format Get-FileHash de Deno");

        // Cas refusés : rien à vérifier = échec de l'installation (on ne garde jamais un outil non vérifié).
        Egal(null, Outils.TrouverEmpreinte(ytdlp, "absent.exe"), "fichier absent de la liste");
        Egal(null, Outils.TrouverEmpreinte("", "yt-dlp.exe"), "fichier d'empreintes vide");
        Egal(null, Outils.TrouverEmpreinte("<html>Not Found</html>", "yt-dlp.exe"), "page d'erreur au lieu des empreintes");
        Egal(null, Outils.TrouverEmpreinte("Hash : " + a + "\nPath : autre-fichier.zip", "deno-x86_64-pc-windows-msvc.zip"), "empreinte unique mais pour un autre fichier");
        Egal(null, Outils.TrouverEmpreinte(a + "\n" + b + "\n", "outil.zip"), "plusieurs empreintes sans nom : ambigu");

        // Calcul SHA-256 (vecteur de test officiel : « abc »).
        string fichier = Path.Combine(Path.GetTempPath(), "MusiquePourAuto-sha-" + Guid.NewGuid().ToString("N"));
        try
        {
            File.WriteAllBytes(fichier, new byte[] { 0x61, 0x62, 0x63 });
            Egal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", Outils.Sha256Fichier(fichier), "SHA-256 de « abc »");
        }
        finally
        {
            File.Delete(fichier);
        }
    }

    static void TesterCopie()
    {
        string racineTest = Path.Combine(Path.GetTempPath(), "MusiquePourAuto-test-" + Guid.NewGuid().ToString("N"));
        string source = Path.Combine(racineTest, "pc");
        string cle = Path.Combine(racineTest, "cle");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(cle);
        try
        {
            Action<int, string> rien = delegate { };
            var vide = CleUsb.CopierVers(cle, 1L << 40, source, 3, rien);
            Vrai(!vide.Reussi && vide.Message.Contains("aucune chanson"), "copie sans chanson");

            for (int i = 1; i <= 5; i++)
                File.WriteAllBytes(Path.Combine(source, "Chanson " + i + ".mp3"), new byte[1000 + i]);
            File.WriteAllText(Path.Combine(source, "notes.txt"), "pas une chanson");

            var r1 = CleUsb.CopierVers(cle, 1L << 40, source, 3, rien);
            Vrai(r1.Reussi, "première copie réussie");
            Egal("5", r1.Copiees.ToString(), "5 chansons copiées");
            Egal("3", Directory.GetFiles(Path.Combine(cle, "Musique")).Length.ToString(), "3 fichiers max dans Musique");
            Egal("2", Directory.GetFiles(Path.Combine(cle, "Musique 2")).Length.ToString(), "le reste dans Musique 2");
            Vrai(!File.Exists(Path.Combine(cle, "Musique", "notes.txt")) && !File.Exists(Path.Combine(cle, "Musique 2", "notes.txt")), "seuls les MP3 sont copiés");
            Egal("1001", new FileInfo(Path.Combine(cle, "Musique", "Chanson 1.mp3")).Length.ToString(), "contenu copié intact");
            Egal("0", Directory.GetFiles(cle, "*.partiel", SearchOption.AllDirectories).Length.ToString(), "aucun fichier partiel restant");

            var r2 = CleUsb.CopierVers(cle, 1L << 40, source, 3, rien);
            Vrai(r2.Reussi && r2.Copiees == 0 && r2.Message.Contains("déjà"), "deuxième copie : rien à refaire");

            File.WriteAllBytes(Path.Combine(source, "Chanson 6.mp3"), new byte[2000]);
            var r3 = CleUsb.CopierVers(cle, 1L << 40, source, 3, rien);
            Egal("1", r3.Copiees.ToString(), "seule la nouvelle chanson est copiée");
            Vrai(File.Exists(Path.Combine(cle, "Musique 2", "Chanson 6.mp3")), "nouvelle chanson dans le dernier dossier non plein");

            File.WriteAllBytes(Path.Combine(source, "Chanson 7.mp3"), new byte[5000]);
            var r4 = CleUsb.CopierVers(cle, 100, source, 3, rien);
            Vrai(!r4.Reussi && r4.Message.Contains("pas assez de place"), "clé pleine détectée");
        }
        finally
        {
            try { Directory.Delete(racineTest, true); } catch { }
        }
    }

    static string Nom(string titre, string track, string artist, string chaine)
    {
        return NomsChansons.NomDeFichier(NomsChansons.Deduire(titre, track, artist, chaine), "abc123");
    }

    static int Main()
    {
        // Titres de vidéoclips typiques
        Egal("Queen - Bohemian Rhapsody.mp3", Nom("Queen – Bohemian Rhapsody (Official Video Remastered)", "", "", "Queen Official"), "tiret demi-cadratin + (Official Video)");
        Egal("Bon Jovi - Livin' On A Prayer.mp3", Nom("Bon Jovi - Livin' On A Prayer", "", "", "Bon Jovi"), "titre simple");
        Egal("Daft Punk - Get Lucky (feat. Pharrell Williams).mp3", Nom("Daft Punk - Get Lucky (Official Audio) ft. Pharrell Williams, Nile Rodgers", "Get Lucky (feat. Pharrell Williams)", "Daft Punk", "Daft Punk"), "YouTube Music track/artist prioritaires");
        Egal("Mes Aïeux - Dégénérations.mp3", Nom("Mes Aïeux - Dégénérations [Vidéoclip officiel]", "", "", "Mes Aïeux"), "accents conservés + vidéoclip officiel");
        Egal("Céline Dion - Pour que tu m'aimes encore.mp3", Nom("Céline Dion - Pour que tu m'aimes encore (Clip officiel) [HD]", "", "", "CelineDionVEVO"), "plusieurs groupes parasites");
        Egal("Eagles - Hotel California (Live 1977).mp3", Nom("Eagles - Hotel California (Live 1977) (Official Video) [HD]", "", "", "Eagles"), "(Live) conservé");
        Egal("Adele - Hello.mp3", Nom("Hello", "", "", "AdeleVEVO"), "artiste tiré de la chaîne VEVO");
        Egal("Johnny Cash - Hurt.mp3", Nom("Hurt", "Hurt", "Johnny Cash", "Johnny Cash - Topic"), "chaîne Topic");
        Egal("Metallica - Nothing Else Matters.mp3", Nom("Metallica: Nothing Else Matters (Official Music Video)", "", "", "Metallica"), "Artiste: Titre quand l'artiste est le nom de la chaîne");
        Egal("AC-DC - Thunderstruck.mp3", Nom("AC/DC - Thunderstruck (Official Video)", "", "", "acdcVEVO"), "barre oblique interdite");
        Egal("Shania Twain - Man! I Feel Like A Woman!.mp3", Nom("Shania Twain - Man! I Feel Like A Woman! | Official Music Video", "", "", "ShaniaTwainVEVO"), "partie après | retirée");
        Egal("Toto - Africa.mp3", Nom("Toto - \"Africa\" (Lyrics) 🎵🔥", "", "", "Toto"), "émojis et guillemets retirés");
        Egal("Garth Brooks - Friends in Low Places.mp3", Nom("Friends in Low Places", "", "", "Garth Brooks Official"), "Official retiré du nom de chaîne");
        Egal("Chanson abc123.mp3", Nom("🎵🎵", "", "", ""), "titre vide -> nom de secours");
        Egal("_CON.mp3", Nom("CON", "", "", ""), "nom réservé Windows");

        // Caractères interdits
        Egal("Qui - Quoi 'ok'", NomsChansons.NettoyerPourFichier("Qui: Quoi? \"ok\" *<>"), "caractères FAT32 interdits");
        Egal("Fin", NomsChansons.NettoyerPourFichier("Fin..."), "points finaux");

        // Longueur
        string longTitre = "Artiste - " + new string('a', 200);
        string nomLong = Nom(longTitre, "", "", "");
        Vrai(nomLong.Length <= NomsChansons.LongueurMaxNom + 4, "nom tronqué à 100 caractères");

        // Liens
        Vrai(NomsChansons.EstUnLienYouTube("https://www.youtube.com/watch?v=jNQXAC9IVRw"), "lien youtube.com");
        Vrai(NomsChansons.EstUnLienYouTube("https://youtu.be/jNQXAC9IVRw?si=xyz"), "lien youtu.be");
        Vrai(NomsChansons.EstUnLienYouTube("https://music.youtube.com/watch?v=jNQXAC9IVRw&list=RDAMVM"), "lien YouTube Music");
        Vrai(NomsChansons.EstUnLienYouTube("  https://m.youtube.com/shorts/abc  "), "lien mobile avec espaces");
        Vrai(!NomsChansons.EstUnLienYouTube("Bohemian Rhapsody"), "texte qui n'est pas un lien");
        Vrai(!NomsChansons.EstUnLienYouTube("https://notyoutube.com/watch?v=1"), "faux domaine");
        Vrai(!NomsChansons.RessembleAUnLien("ftp://exemple.com"), "ftp refusé");

        // Traduction des erreurs de yt-dlp
        Erreur("ERROR: [youtube] xxxxxxxxxxx: This video is unavailable", "pas disponible", false);
        Erreur("ERROR: [youtube] abc: Video unavailable. This video has been removed by the uploader", "pas disponible", false);
        Erreur("ERROR: [youtube] abc: Private video. Sign in if you've been granted access to this video", "privée", false);
        Erreur("ERROR: [youtube] abc: Sign in to confirm your age. This video may be inappropriate for some users.", "adultes", false);
        Erreur("ERROR: [generic] 'foo' is not a valid URL", "pas reconnu", false);
        Erreur("ERROR: [youtube] abc: Unable to download API page: <urlopen error [Errno 11001] getaddrinfo failed>", "Internet", false);
        Erreur("ERROR: [youtube] abc: Sign in to confirm you’re not a bot.", "bloque temporairement", true);
        Erreur("ERROR: [youtube] abc: Requested format is not available", "n'a pas fonctionné", true);
        Erreur("[download] Bla bla does not pass filter (live_status!=is_live), skipping ..", "direct", false);

        TesterCopie();
        TesterEmpreintes();

        Console.WriteLine("{0} tests, {1} échec(s)", total, echecs);
        return echecs == 0 ? 0 : 1;
    }
}
