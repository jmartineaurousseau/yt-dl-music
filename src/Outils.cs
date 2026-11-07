// Emplacements, journal, lancement de programmes et installation automatique
// des outils externes (yt-dlp, ffmpeg, deno) dans %LOCALAPPDATA%\MusiquePourAuto.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;

namespace MusiquePourAuto
{
    public static class Emplacements
    {
        public static string Accueil
        {
            get
            {
                string force = Environment.GetEnvironmentVariable("MUSIQUEAUTO_HOME");
                if (!string.IsNullOrEmpty(force)) return force;
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MusiquePourAuto");
            }
        }

        public static string DossierOutils { get { return Path.Combine(Accueil, "outils"); } }
        public static string YtDlp { get { return Path.Combine(DossierOutils, "yt-dlp.exe"); } }
        public static string DossierFfmpeg { get { return Path.Combine(DossierOutils, "ffmpeg"); } }
        public static string Ffmpeg { get { return Path.Combine(DossierFfmpeg, "ffmpeg.exe"); } }
        public static string Ffprobe { get { return Path.Combine(DossierFfmpeg, "ffprobe.exe"); } }
        public static string Deno { get { return Path.Combine(DossierOutils, "deno.exe"); } }
        public static string Journal { get { return Path.Combine(Accueil, "journal.txt"); } }
        public static string MarqueurMiseAJour { get { return Path.Combine(Accueil, "derniere-mise-a-jour.txt"); } }

        /// <summary>Dossier où les chansons sont enregistrées : Musique\Musique pour l'auto.</summary>
        public static string DossierChansons
        {
            get
            {
                string force = Environment.GetEnvironmentVariable("MUSIQUEAUTO_MUSIQUE");
                if (!string.IsNullOrEmpty(force)) return force;
                string musique = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                if (string.IsNullOrEmpty(musique))
                    musique = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Music");
                return Path.Combine(musique, "Musique pour l'auto");
            }
        }

        public static string DossierTemporaire
        {
            get { return Path.Combine(Path.GetTempPath(), "MusiquePourAuto"); }
        }
    }

    /// <summary>Journal texte pour diagnostiquer un problème à distance.</summary>
    public static class Journal
    {
        static readonly object verrou = new object();

        public static void Ecrire(string message)
        {
            try
            {
                lock (verrou)
                {
                    Directory.CreateDirectory(Emplacements.Accueil);
                    var info = new FileInfo(Emplacements.Journal);
                    if (info.Exists && info.Length > 2 * 1024 * 1024)
                        File.Delete(Emplacements.Journal);
                    File.AppendAllText(Emplacements.Journal,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + Environment.NewLine,
                        Encoding.UTF8);
                }
            }
            catch
            {
                // Le journal ne doit jamais faire planter l'application.
            }
        }
    }

    public sealed class ResultatProgramme
    {
        public int CodeSortie;
        public string Sortie = "";
        public string Erreurs = "";
        public bool DelaiDepasse;
    }

    /// <summary>Lance un programme sans fenêtre noire et récupère sa sortie (UTF-8).</summary>
    public static class Programme
    {
        static readonly List<Process> enCours = new List<Process>();

        /// <summary>Arrête yt-dlp / ffmpeg si l'utilisateur ferme la fenêtre pendant un travail.</summary>
        public static void ArreterTout()
        {
            lock (enCours)
            {
                foreach (var p in enCours)
                {
                    try { if (!p.HasExited) p.Kill(); } catch { }
                }
            }
        }

        public static ResultatProgramme Lancer(string exe, IList<string> arguments, Action<string> ligneSortie, int delaiMs)
        {
            var info = new ProcessStartInfo(exe, ConstruireLigne(arguments));
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.StandardOutputEncoding = Encoding.UTF8;
            info.StandardErrorEncoding = Encoding.UTF8;
            info.WorkingDirectory = Path.GetDirectoryName(exe);
            info.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            info.EnvironmentVariables["NO_COLOR"] = "1";

            var resultat = new ResultatProgramme();
            var sortie = new StringBuilder();
            var erreurs = new StringBuilder();

            using (var p = new Process())
            {
                p.StartInfo = info;
                p.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
                {
                    if (e.Data == null) return;
                    lock (sortie) sortie.AppendLine(e.Data);
                    if (ligneSortie != null) ligneSortie(e.Data);
                };
                p.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e)
                {
                    if (e.Data == null) return;
                    lock (erreurs) erreurs.AppendLine(e.Data);
                };
                p.Start();
                lock (enCours) enCours.Add(p);
                try
                {
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    if (!p.WaitForExit(delaiMs))
                    {
                        resultat.DelaiDepasse = true;
                        try { p.Kill(); } catch { }
                        p.WaitForExit(5000);
                    }
                    p.WaitForExit(); // vide les tampons de sortie
                    resultat.CodeSortie = resultat.DelaiDepasse ? -1 : p.ExitCode;
                }
                finally
                {
                    lock (enCours) enCours.Remove(p);
                }
            }
            resultat.Sortie = sortie.ToString();
            resultat.Erreurs = erreurs.ToString();
            return resultat;
        }

        /// <summary>Met chaque argument entre guillemets selon les règles de Windows.</summary>
        public static string ConstruireLigne(IList<string> arguments)
        {
            var sb = new StringBuilder();
            foreach (string arg in arguments)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(Citer(arg));
            }
            return sb.ToString();
        }

        static string Citer(string arg)
        {
            if (arg.Length > 0 && arg.IndexOfAny(new[] { ' ', '\t', '"', '&', '|', '<', '>', '^' }) < 0)
                return arg;
            var sb = new StringBuilder("\"");
            int barres = 0;
            foreach (char c in arg)
            {
                if (c == '\\') { barres++; continue; }
                if (c == '"')
                {
                    sb.Append('\\', barres * 2 + 1);
                    sb.Append('"');
                }
                else
                {
                    sb.Append('\\', barres);
                    sb.Append(c);
                }
                barres = 0;
            }
            sb.Append('\\', barres * 2);
            sb.Append('"');
            return sb.ToString();
        }
    }

    /// <summary>Installation et mise à jour des outils nécessaires.</summary>
    public static class Outils
    {
        const string UrlYtDlp = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
        const string UrlFfmpeg = "https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl-shared.zip";
        const string UrlDeno = "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip";

        /// <summary>Empêche une mise à jour de yt-dlp pendant un téléchargement.</summary>
        public static readonly object VerrouYtDlp = new object();

        public static bool SontInstalles
        {
            get
            {
                return File.Exists(Emplacements.YtDlp)
                    && File.Exists(Emplacements.Ffmpeg)
                    && File.Exists(Emplacements.Deno);
            }
        }

        /// <summary>
        /// Télécharge ce qui manque. progression(pourcentage 0-100, message).
        /// Lance une exception avec un message en français si ça échoue.
        /// </summary>
        public static void Installer(Action<int, string> progression)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Directory.CreateDirectory(Emplacements.DossierOutils);

            // Poids approximatifs pour une barre de progression globale réaliste.
            const int poidsYtDlp = 15, poidsFfmpeg = 60, poidsDeno = 25;
            int fait = 0;

            if (!File.Exists(Emplacements.YtDlp))
            {
                Journal.Ecrire("Installation de yt-dlp");
                int depart = fait;
                Telecharger(UrlYtDlp, Emplacements.YtDlp, delegate(int p)
                {
                    progression(depart + p * poidsYtDlp / 100, "Préparation (1 de 3) : outil de téléchargement...");
                });
            }
            fait += poidsYtDlp;

            if (!File.Exists(Emplacements.Ffmpeg) || !File.Exists(Emplacements.Ffprobe))
            {
                Journal.Ecrire("Installation de ffmpeg");
                string zip = Path.Combine(Emplacements.DossierOutils, "ffmpeg.zip");
                int depart = fait;
                Telecharger(UrlFfmpeg, zip, delegate(int p)
                {
                    progression(depart + p * (poidsFfmpeg - 5) / 100, "Préparation (2 de 3) : outil de conversion MP3...");
                });
                progression(fait + poidsFfmpeg - 5, "Préparation (2 de 3) : installation de l'outil de conversion...");
                ExtraireDossierBin(zip, Emplacements.DossierFfmpeg);
                File.Delete(zip);
            }
            fait += poidsFfmpeg;

            if (!File.Exists(Emplacements.Deno))
            {
                Journal.Ecrire("Installation de deno");
                string zip = Path.Combine(Emplacements.DossierOutils, "deno.zip");
                int depart = fait;
                Telecharger(UrlDeno, zip, delegate(int p)
                {
                    progression(depart + p * (poidsDeno - 3) / 100, "Préparation (3 de 3) : outil de compatibilité YouTube...");
                });
                ExtraireFichier(zip, "deno.exe", Emplacements.Deno);
                File.Delete(zip);
            }

            progression(100, "Préparation terminée.");
            Journal.Ecrire("Outils installés");
            File.WriteAllText(Emplacements.MarqueurMiseAJour, DateTime.Now.ToString("yyyy-MM-dd"));
        }

        /// <summary>
        /// Met yt-dlp à jour (YouTube change souvent, une vieille version cesse de fonctionner).
        /// Retourne true si la commande a réussi. Ne lance jamais d'exception.
        /// </summary>
        public static bool MettreAJourYtDlp()
        {
            try
            {
                if (!File.Exists(Emplacements.YtDlp)) return false;
                lock (VerrouYtDlp)
                {
                    var r = Programme.Lancer(Emplacements.YtDlp, new[] { "-U" }, null, 120000);
                    Journal.Ecrire("Mise à jour de yt-dlp (code " + r.CodeSortie + ") : " + (r.Sortie + r.Erreurs).Trim().Replace(Environment.NewLine, " / "));
                    if (r.CodeSortie == 0)
                        File.WriteAllText(Emplacements.MarqueurMiseAJour, DateTime.Now.ToString("yyyy-MM-dd"));
                    return r.CodeSortie == 0;
                }
            }
            catch (Exception ex)
            {
                Journal.Ecrire("Mise à jour impossible : " + ex.Message);
                return false;
            }
        }

        /// <summary>Vrai si la dernière mise à jour réussie date d'avant aujourd'hui.</summary>
        public static bool MiseAJourDue()
        {
            try
            {
                if (!File.Exists(Emplacements.MarqueurMiseAJour)) return true;
                return File.ReadAllText(Emplacements.MarqueurMiseAJour).Trim() != DateTime.Now.ToString("yyyy-MM-dd");
            }
            catch
            {
                return true;
            }
        }

        static void Telecharger(string url, string destination, Action<int> progression)
        {
            string partiel = destination + ".partiel";
            Exception derniere = null;
            for (int essai = 1; essai <= 3; essai++)
            {
                try
                {
                    var requete = (HttpWebRequest)WebRequest.Create(url);
                    requete.UserAgent = "MusiquePourAuto/1.0";
                    requete.AllowAutoRedirect = true;
                    requete.Timeout = 60000;
                    requete.ReadWriteTimeout = 60000;
                    using (var reponse = (HttpWebResponse)requete.GetResponse())
                    using (var flux = reponse.GetResponseStream())
                    using (var fichier = new FileStream(partiel, FileMode.Create, FileAccess.Write))
                    {
                        long total = reponse.ContentLength;
                        long lu = 0;
                        var tampon = new byte[81920];
                        int n, dernierPourcent = -1;
                        while ((n = flux.Read(tampon, 0, tampon.Length)) > 0)
                        {
                            fichier.Write(tampon, 0, n);
                            lu += n;
                            if (total > 0)
                            {
                                int pourcent = (int)(lu * 100 / total);
                                if (pourcent != dernierPourcent)
                                {
                                    dernierPourcent = pourcent;
                                    progression(pourcent);
                                }
                            }
                        }
                        if (total > 0 && lu != total)
                            throw new IOException("Téléchargement incomplet (" + lu + " sur " + total + " octets)");
                    }
                    if (File.Exists(destination)) File.Delete(destination);
                    File.Move(partiel, destination);
                    return;
                }
                catch (Exception ex)
                {
                    derniere = ex;
                    Journal.Ecrire("Échec du téléchargement de " + url + " (essai " + essai + ") : " + ex.Message);
                    try { if (File.Exists(partiel)) File.Delete(partiel); } catch { }
                    if (essai < 3) Thread.Sleep(3000 * essai);
                }
            }
            throw new InvalidOperationException(
                "Impossible de télécharger les outils nécessaires. Vérifiez que l'ordinateur est bien connecté à Internet, puis réessayez.",
                derniere);
        }

        /// <summary>Copie le contenu du dossier « bin » de l'archive ffmpeg (ffmpeg.exe, ffprobe.exe et leurs DLL).</summary>
        static void ExtraireDossierBin(string zip, string destination)
        {
            string temporaire = destination + ".nouveau";
            if (Directory.Exists(temporaire)) Directory.Delete(temporaire, true);
            Directory.CreateDirectory(temporaire);
            using (var archive = ZipFile.OpenRead(zip))
            {
                foreach (var entree in archive.Entries)
                {
                    string chemin = entree.FullName.Replace('\\', '/');
                    int i = chemin.IndexOf("/bin/", StringComparison.OrdinalIgnoreCase);
                    if (i < 0 || string.IsNullOrEmpty(entree.Name)) continue;
                    string relatif = chemin.Substring(i + 5);
                    if (relatif.Contains("/") || relatif.Contains("..")) continue;
                    entree.ExtractToFile(Path.Combine(temporaire, relatif), true);
                }
            }
            if (!File.Exists(Path.Combine(temporaire, "ffmpeg.exe")))
                throw new InvalidOperationException("L'outil de conversion téléchargé est incomplet. Réessayez plus tard.");
            if (Directory.Exists(destination)) Directory.Delete(destination, true);
            Directory.Move(temporaire, destination);
        }

        static void ExtraireFichier(string zip, string nom, string destination)
        {
            using (var archive = ZipFile.OpenRead(zip))
            {
                foreach (var entree in archive.Entries)
                {
                    if (string.Equals(entree.Name, nom, StringComparison.OrdinalIgnoreCase))
                    {
                        entree.ExtractToFile(destination + ".partiel", true);
                        if (File.Exists(destination)) File.Delete(destination);
                        File.Move(destination + ".partiel", destination);
                        return;
                    }
                }
            }
            throw new InvalidOperationException("L'archive téléchargée ne contient pas " + nom + ".");
        }
    }
}
