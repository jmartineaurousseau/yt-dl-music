// Téléchargement d'une vidéo YouTube (audio seulement) puis conversion en MP3
// compatible avec les radios d'automobile (Toyota Entune, Jeep Uconnect, etc.) :
//   - MP3 MPEG-1 Layer III, débit CONSTANT 192 kb/s (le débit variable fausse
//     la durée ou fait sauter certaines radios)
//   - 44 100 Hz, stéréo (YouTube fournit souvent du 48 000 Hz)
//   - étiquettes ID3v2.3 + ID3v1 (beaucoup de radios ne lisent pas l'ID3v2.4)
//   - aucune image intégrée ni longue description (peuvent bloquer la lecture)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace MusiquePourAuto
{
    public sealed class ResultatConversion
    {
        public bool Reussi;
        public string Message = "";
        public string Fichier = "";
        public InfosChanson Infos;
    }

    public static class Conversion
    {
        public const string DebitMp3 = "192k";
        public const string Frequence = "44100";

        sealed class InfosTelechargement
        {
            public string Fichier = "", Id = "", Titre = "", Track = "", Artist = "", Chaine = "";
            public double Duree;
        }

        /// <summary>progression(pourcentage 0-100 ou -1 si inconnu, message)</summary>
        public static ResultatConversion Convertir(string lien, string dossierDestination, Action<int, string> progression)
        {
            lien = (lien ?? "").Trim();
            if (!NomsChansons.RessembleAUnLien(lien))
                return Echec("Ce texte n'est pas un lien. Dans YouTube, cliquez sur « Partager » puis « Copier », et collez le lien ici.");

            string dossierTravail = Path.Combine(Emplacements.DossierTemporaire, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dossierTravail);
            try
            {
                Journal.Ecrire("Nouveau téléchargement : " + lien);
                progression(-1, "Recherche de la vidéo...");

                ResultatProgramme r = null;
                InfosTelechargement infos = null;
                for (int essai = 1; essai <= 2; essai++)
                {
                    infos = new InfosTelechargement();
                    lock (Outils.VerrouYtDlp)
                    {
                        r = LancerYtDlp(lien, dossierTravail, infos, progression);
                    }
                    if (r.CodeSortie == 0 && File.Exists(infos.Fichier)) break;

                    Journal.Ecrire("yt-dlp a échoué (code " + r.CodeSortie + ") :" + Environment.NewLine + r.Erreurs);
                    bool miseAJourUtile;
                    string explication = ExpliquerErreur(r.Erreurs + r.Sortie, out miseAJourUtile);
                    if (essai == 1 && miseAJourUtile)
                    {
                        progression(-1, "Mise à jour de l'outil de téléchargement, un instant...");
                        if (Outils.MettreAJourYtDlp())
                        {
                            progression(-1, "Nouvel essai...");
                            continue;
                        }
                    }
                    return Echec(explication);
                }

                InfosChanson chanson = NomsChansons.Deduire(infos.Titre, infos.Track, infos.Artist, infos.Chaine);
                string nomFichier = NomsChansons.NomDeFichier(chanson, infos.Id);
                Directory.CreateDirectory(dossierDestination);
                string destination = Path.Combine(dossierDestination, nomFichier);
                string mp3Temporaire = Path.Combine(dossierTravail, "conversion.mp3");

                progression(75, "Conversion en MP3...");
                var conversion = LancerFfmpeg(infos.Fichier, mp3Temporaire, chanson, infos.Duree, progression);
                if (conversion.CodeSortie != 0 || !File.Exists(mp3Temporaire) || new FileInfo(mp3Temporaire).Length < 1000)
                {
                    Journal.Ecrire("ffmpeg a échoué (code " + conversion.CodeSortie + ") :" + Environment.NewLine + conversion.Erreurs);
                    return Echec("La conversion en MP3 n'a pas fonctionné. Essayez une autre vidéo de la même chanson.");
                }

                if (File.Exists(destination)) File.Delete(destination);
                File.Move(mp3Temporaire, destination);
                Journal.Ecrire("Chanson prête : " + destination);

                progression(100, "Terminé !");
                var ok = new ResultatConversion();
                ok.Reussi = true;
                ok.Fichier = destination;
                ok.Infos = chanson;
                ok.Message = "C'est prêt ! La chanson « " + Path.GetFileNameWithoutExtension(destination) + " » a été ajoutée à vos chansons.";
                return ok;
            }
            catch (Exception ex)
            {
                Journal.Ecrire("Erreur inattendue : " + ex);
                if (ex is UnauthorizedAccessException || ex is IOException)
                    return Echec("Impossible d'enregistrer la chanson. Le fichier est peut-être ouvert dans un autre programme. Fermez-le et réessayez.");
                return Echec("Un problème inattendu est survenu. Réessayez. (" + ex.Message + ")");
            }
            finally
            {
                try { Directory.Delete(dossierTravail, true); } catch { }
            }
        }

        static ResultatProgramme LancerYtDlp(string lien, string dossierTravail, InfosTelechargement infos, Action<int, string> progression)
        {
            var args = new List<string>
            {
                "--ignore-config",
                "--no-playlist",                 // un lien « &list=... » ne télécharge que la vidéo choisie
                "--encoding", "utf-8",
                "--color", "never",
                "--newline", "--progress", "--no-simulate",
                "--no-mtime",
                "--match-filter", "live_status!=is_live & live_status!=is_upcoming",
                "-f", "bestaudio/best",
                "--ffmpeg-location", Emplacements.DossierFfmpeg,
                "--js-runtimes", "deno:" + Emplacements.Deno,
                "--progress-template", "download:@@PROG %(progress.downloaded_bytes|0)s %(progress.total_bytes|0)s %(progress.total_bytes_estimate|0)s",
                "--print", "after_move:@@FILE %(filepath)s",
                "--print", "after_move:@@ID %(id|)s",
                "--print", "after_move:@@TITLE %(title|)s",
                "--print", "after_move:@@TRACK %(track|)s",
                "--print", "after_move:@@ARTIST %(artist|)s",
                "--print", "after_move:@@CHAINE %(uploader|)s",
                "--print", "after_move:@@DUREE %(duration|0)s",
                "-o", Path.Combine(dossierTravail, "%(id)s.%(ext)s"),
                "--", lien
            };

            int dernier = -2;
            return Programme.Lancer(Emplacements.YtDlp, args, delegate(string ligne)
            {
                if (ligne.StartsWith("@@PROG "))
                {
                    string[] p = ligne.Substring(7).Split(' ');
                    if (p.Length < 3) return;
                    double fait = Nombre(p[0]), total = Nombre(p[1]);
                    if (total <= 0) total = Nombre(p[2]);
                    if (total <= 0) return;
                    int pourcent = (int)Math.Min(100, fait * 100 / total);
                    if (pourcent == dernier) return;
                    dernier = pourcent;
                    progression(pourcent * 75 / 100, "Téléchargement de la chanson... " + pourcent + " %");
                }
                else if (ligne.StartsWith("@@FILE ")) infos.Fichier = ligne.Substring(7).Trim();
                else if (ligne.StartsWith("@@ID ")) infos.Id = ligne.Substring(5).Trim();
                else if (ligne.StartsWith("@@TITLE ")) infos.Titre = ligne.Substring(8).Trim();
                else if (ligne.StartsWith("@@TRACK ")) infos.Track = ligne.Substring(8).Trim();
                else if (ligne.StartsWith("@@ARTIST ")) infos.Artist = ligne.Substring(9).Trim();
                else if (ligne.StartsWith("@@CHAINE ")) infos.Chaine = ligne.Substring(9).Trim();
                else if (ligne.StartsWith("@@DUREE ")) infos.Duree = Nombre(ligne.Substring(8));
            }, 30 * 60 * 1000);
        }

        static ResultatProgramme LancerFfmpeg(string source, string destination, InfosChanson chanson, double dureeSecondes, Action<int, string> progression)
        {
            var args = new List<string>
            {
                "-hide_banner", "-nostdin", "-y", "-loglevel", "error",
                "-i", source,
                "-map", "0:a:0", "-vn", "-sn", "-dn",
                "-map_metadata", "-1", "-map_chapters", "-1",
                "-c:a", "libmp3lame", "-b:a", DebitMp3, "-ar", Frequence, "-ac", "2",
                "-id3v2_version", "3", "-write_id3v1", "1",
                "-metadata", "title=" + chanson.Titre,
                "-metadata", "artist=" + chanson.Artiste,
                "-progress", "pipe:1", "-nostats",
                destination
            };
            int dernier = -1;
            return Programme.Lancer(Emplacements.Ffmpeg, args, delegate(string ligne)
            {
                if (dureeSecondes <= 0 || !ligne.StartsWith("out_time_us=")) return;
                double us = Nombre(ligne.Substring(12));
                int pourcent = (int)Math.Max(0, Math.Min(100, us / 10000.0 / dureeSecondes));
                if (pourcent == dernier) return;
                dernier = pourcent;
                progression(75 + pourcent * 25 / 100, "Conversion en MP3... " + pourcent + " %");
            }, 30 * 60 * 1000);
        }

        /// <summary>Traduit les erreurs de yt-dlp en une phrase simple.</summary>
        public static string ExpliquerErreur(string erreurs, out bool miseAJourUtile)
        {
            string e = erreurs ?? "";
            miseAJourUtile = false;

            if (Contient(e, "Unsupported URL", "is not a valid URL", "Incomplete YouTube ID", "Invalid URL"))
                return "Ce lien n'est pas reconnu. Vérifiez qu'il s'agit bien du lien d'une vidéo YouTube.";
            if (Contient(e, "Private video"))
                return "Cette vidéo est privée : impossible de la télécharger. Cherchez une autre vidéo de la même chanson.";
            if (Contient(e, "confirm your age", "age-restricted", "inappropriate for some users"))
                return "YouTube réserve cette vidéo aux adultes connectés. Cherchez une autre vidéo de la même chanson.";
            if (Contient(e, "members-only", "Join this channel", "available to this channel's members"))
                return "Cette vidéo est réservée aux membres de la chaîne. Cherchez une autre vidéo de la même chanson.";
            if (Contient(e, "does not pass filter"))
                return "C'est une diffusion en direct (ou à venir). Choisissez une vidéo ordinaire de la chanson.";
            if (Contient(e, "Video unavailable", "video is unavailable", "This video has been removed", "video is not available", "content isn't available", "copyright claim", "not made this video available in your country"))
                return "Cette vidéo n'est pas disponible (supprimée ou bloquée). Cherchez une autre vidéo de la même chanson.";
            if (Contient(e, "getaddrinfo", "Failed to resolve", "Name or service not known", "No address associated", "Network is unreachable",
                            "timed out", "Connection refused", "Connection reset", "WinError 10060", "WinError 10051", "WinError 11001", "Temporary failure in name resolution"))
                return "Impossible de joindre YouTube. Vérifiez que l'ordinateur est connecté à Internet, puis réessayez.";
            if (Contient(e, "not a bot", "rate-limited", "HTTP Error 429"))
            {
                miseAJourUtile = true;
                return "YouTube bloque temporairement les téléchargements depuis cet ordinateur. Réessayez dans une heure environ.";
            }

            miseAJourUtile = true;
            return "Le téléchargement n'a pas fonctionné. Vérifiez le lien et la connexion Internet, puis réessayez dans quelques minutes.";
        }

        static bool Contient(string texte, params string[] morceaux)
        {
            foreach (string m in morceaux)
                if (texte.IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        static double Nombre(string texte)
        {
            double v;
            return double.TryParse((texte ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : 0;
        }

        static ResultatConversion Echec(string message)
        {
            var r = new ResultatConversion();
            r.Reussi = false;
            r.Message = message;
            return r;
        }
    }
}
