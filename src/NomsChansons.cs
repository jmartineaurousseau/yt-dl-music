// Nettoyage des titres YouTube et fabrication de noms de fichiers
// compatibles avec une clé USB FAT32 et les radios d'automobile.
// Fonctions pures, sans dépendance : testées par tests\Tests.cs.

using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MusiquePourAuto
{
    public sealed class InfosChanson
    {
        public string Artiste;
        public string Titre;

        public InfosChanson(string artiste, string titre)
        {
            Artiste = artiste ?? "";
            Titre = titre ?? "";
        }
    }

    public static class NomsChansons
    {
        // Longueur max. du nom de fichier (sans « .mp3 »). FAT32 accepte 255,
        // mais les radios d'auto affichent mal les noms très longs.
        public const int LongueurMaxNom = 100;

        // Mots qui signalent un ajout inutile entre parenthèses ou crochets :
        // « (Official Video) », « [Lyrics] », « (Vidéoclip officiel) », « [HD] »...
        static readonly Regex MotsParasites = new Regex(
            @"\b(official|officiel(le)?|vid[eé]o(clip)?s?|clip|lyrics?|paroles|audio|visuali[sz]er|hd|hq|4k|1080p|720p|mv|m/v|explicit|explicite)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        static readonly Regex GroupeEntreParentheses = new Regex(
            @"\s*[\(\[【]([^\(\)\[\]【】]*)[\)\]】]",
            RegexOptions.CultureInvariant);

        static readonly Regex SeparateurArtisteTitre = new Regex(
            @"\s+[-–—~]\s+", RegexOptions.CultureInvariant);

        static readonly Regex Espaces = new Regex(@"\s+", RegexOptions.CultureInvariant);

        static readonly string[] NomsReserves =
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        /// <summary>
        /// Déduit l'artiste et le titre à partir des informations de YouTube.
        /// track/artist viennent de YouTube Music (souvent vides pour les vidéoclips).
        /// </summary>
        public static InfosChanson Deduire(string titreVideo, string track, string artist, string chaine)
        {
            string artiste = NettoyerTexte(artist);
            string titre = NettoyerTexte(track);

            // Chanson reconnue par YouTube Music : on se fie à ses informations.
            if (artiste.Length > 0 && titre.Length > 0)
                return new InfosChanson(artiste, RetirerParasites(titre));

            string t = RetirerParasites(NettoyerTexte(titreVideo));

            // « Artiste - Chanson | Official Video » : on garde la partie avant la barre
            // seulement si la partie après ressemble à du bruit.
            int barre = t.IndexOf('|');
            if (barre > 0)
            {
                string apres = t.Substring(barre + 1);
                if (MotsParasites.IsMatch(apres) || apres.Trim().Length == 0)
                    t = t.Substring(0, barre).Trim();
            }

            Match sep = SeparateurArtisteTitre.Match(t);
            if (sep.Success && sep.Index > 0 && sep.Index + sep.Length < t.Length)
            {
                string gauche = t.Substring(0, sep.Index).Trim();
                string droite = t.Substring(sep.Index + sep.Length).Trim();
                return new InfosChanson(RetirerGuillemets(gauche), RetirerGuillemets(droite));
            }

            if (artiste.Length == 0)
                artiste = NettoyerNomChaine(chaine);

            // « Metallica: Nothing Else Matters » quand la chaîne s'appelle Metallica.
            if (artiste.Length > 0)
            {
                Match deuxPoints = Regex.Match(t, "^" + Regex.Escape(artiste) + @"\s*:\s+(.+)$", RegexOptions.IgnoreCase);
                if (deuxPoints.Success)
                    return new InfosChanson(t.Substring(0, artiste.Length), RetirerGuillemets(deuxPoints.Groups[1].Value));
            }

            return new InfosChanson(artiste, RetirerGuillemets(t));
        }

        /// <summary>Retire « (Official Video) », « [HD] », etc., mais garde « (feat. X) », « (Live) », « (Remix) ».</summary>
        public static string RetirerParasites(string texte)
        {
            if (string.IsNullOrEmpty(texte)) return "";
            string resultat = texte;
            // Plusieurs passes pour « (Official Video) [HD] (Lyrics) ».
            for (int i = 0; i < 5; i++)
            {
                string avant = resultat;
                resultat = GroupeEntreParentheses.Replace(resultat, delegate(Match m)
                {
                    string dedans = m.Groups[1].Value.Trim();
                    if (dedans.Length == 0) return "";
                    if (Regex.IsMatch(dedans, @"^(feat\.?|ft\.?|featuring)\s", RegexOptions.IgnoreCase))
                        return m.Value;
                    return MotsParasites.IsMatch(dedans) ? "" : m.Value;
                });
                if (resultat == avant) break;
            }
            return Espaces.Replace(resultat, " ").Trim();
        }

        /// <summary>« Queen Official », « AdeleVEVO », « Daft Punk - Topic » deviennent le nom de l'artiste.</summary>
        public static string NettoyerNomChaine(string chaine)
        {
            string c = NettoyerTexte(chaine);
            c = Regex.Replace(c, @"\s*-\s*(Topic|Sujet)$", "", RegexOptions.IgnoreCase);
            c = Regex.Replace(c, @"\s*VEVO$", "", RegexOptions.IgnoreCase);
            c = Regex.Replace(c, @"\s*[-|]?\s*(Official|Officiel(le)?)(\s+(Channel|YouTube|Music))?$", "", RegexOptions.IgnoreCase);
            c = Regex.Replace(c, @"\s*(Music|Musique)$", "", RegexOptions.IgnoreCase);
            return c.Trim();
        }

        /// <summary>Nom de fichier final, ex. « Queen - Bohemian Rhapsody.mp3 ».</summary>
        public static string NomDeFichier(InfosChanson infos, string idVideo)
        {
            string artiste = NettoyerPourFichier(infos.Artiste);
            string titre = NettoyerPourFichier(infos.Titre);

            string nom;
            if (titre.Length == 0)
                nom = "Chanson " + NettoyerPourFichier(idVideo);
            else if (artiste.Length == 0 || titre.StartsWith(artiste + " - ", StringComparison.OrdinalIgnoreCase))
                nom = titre;
            else
                nom = artiste + " - " + titre;

            if (nom.Length > LongueurMaxNom)
                nom = nom.Substring(0, LongueurMaxNom);
            nom = nom.TrimEnd(' ', '.', '-').Trim();

            if (nom.Length == 0)
                nom = "Chanson";

            foreach (string reserve in NomsReserves)
            {
                if (string.Equals(nom, reserve, StringComparison.OrdinalIgnoreCase))
                {
                    nom = "_" + nom;
                    break;
                }
            }
            return nom + ".mp3";
        }

        /// <summary>Remplace les caractères interdits sur FAT32 / Windows.</summary>
        public static string NettoyerPourFichier(string texte)
        {
            string t = NettoyerTexte(texte);
            var sb = new StringBuilder(t.Length);
            foreach (char c in t)
            {
                switch (c)
                {
                    case '"': sb.Append('\''); break;
                    case ':': sb.Append(" - "); break;
                    case '/':
                    case '\\':
                    case '|': sb.Append('-'); break;
                    case '?':
                    case '*':
                    case '<':
                    case '>': break;
                    default: sb.Append(c); break;
                }
            }
            string resultat = Espaces.Replace(sb.ToString(), " ");
            resultat = Regex.Replace(resultat, @"-{2,}", "-");
            return resultat.Trim().TrimEnd('.').Trim();
        }

        /// <summary>
        /// Texte propre pour les étiquettes ID3 : sans émoji ni caractères de contrôle,
        /// que plusieurs radios d'auto affichent comme des carrés.
        /// </summary>
        public static string NettoyerTexte(string texte)
        {
            if (string.IsNullOrEmpty(texte)) return "";
            if (texte == "NA") return "";
            var sb = new StringBuilder(texte.Length);
            foreach (char c in texte.Normalize(NormalizationForm.FormC))
            {
                if (char.IsSurrogate(c)) continue;        // émojis
                if (char.IsControl(c)) { sb.Append(' '); continue; }
                UnicodeCategory cat = char.GetUnicodeCategory(c);
                if (cat == UnicodeCategory.OtherSymbol) continue;     // ★ ♫ ✔
                if (cat == UnicodeCategory.Format) continue;          // caractères invisibles
                if (cat == UnicodeCategory.NonSpacingMark) continue;  // sélecteurs de variante
                sb.Append(c);
            }
            return Espaces.Replace(sb.ToString(), " ").Trim();
        }

        static string RetirerGuillemets(string texte)
        {
            string t = texte.Trim();
            if (t.Length >= 2)
            {
                char a = t[0], b = t[t.Length - 1];
                if ((a == '"' && b == '"') || (a == '“' && b == '”') || (a == '«' && b == '»') || (a == '\'' && b == '\''))
                    t = t.Substring(1, t.Length - 2).Trim();
            }
            return t;
        }

        /// <summary>Vérifie grossièrement qu'on a bien un lien web.</summary>
        public static bool RessembleAUnLien(string texte)
        {
            if (string.IsNullOrWhiteSpace(texte)) return false;
            Uri uri;
            return Uri.TryCreate(texte.Trim(), UriKind.Absolute, out uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        public static bool EstUnLienYouTube(string texte)
        {
            if (!RessembleAUnLien(texte)) return false;
            string hote = new Uri(texte.Trim()).Host.ToLowerInvariant();
            return hote == "youtu.be" || hote == "youtube.com" || hote.EndsWith(".youtube.com");
        }
    }
}
