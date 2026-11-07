// Détection des clés USB et copie des chansons dans un dossier « Musique ».

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MusiquePourAuto
{
    public sealed class ResultatCopie
    {
        public bool Reussi;
        public string Message = "";
        public int Copiees;
        public int DejaPresentes;
    }

    public static class CleUsb
    {
        // Plusieurs radios Toyota limitent le nombre de fichiers par dossier (255).
        public const int MaxParDossier = 250;
        const string NomDossier = "Musique";

        public static List<DriveInfo> Lister()
        {
            var cles = new List<DriveInfo>();
            foreach (var d in DriveInfo.GetDrives())
            {
                try
                {
                    if (d.DriveType == DriveType.Removable && d.IsReady)
                        cles.Add(d);
                }
                catch
                {
                    // lecteur qui disparaît pendant la lecture : on l'ignore
                }
            }
            return cles;
        }

        public static string Decrire(DriveInfo d)
        {
            string nom = "";
            try { nom = d.VolumeLabel; } catch { }
            if (string.IsNullOrWhiteSpace(nom)) nom = "Clé USB";
            return string.Format("{0} ({1})  -  {2} libres", nom, d.Name.TrimEnd('\\'), Taille(d.AvailableFreeSpace));
        }

        /// <summary>Message d'avertissement si le format de la clé risque de ne pas être lu par l'auto, sinon null.</summary>
        public static string AvertissementFormat(DriveInfo d)
        {
            string format = "";
            try { format = d.DriveFormat ?? ""; } catch { }
            if (format.Equals("FAT32", StringComparison.OrdinalIgnoreCase) || format.Equals("FAT", StringComparison.OrdinalIgnoreCase))
                return null;
            return "Attention : cette clé USB est en format « " + format + " ». "
                 + "Beaucoup d'autos (dont Toyota et Jeep) ne lisent que les clés en format FAT32. "
                 + "Si la musique ne joue pas dans l'auto, la clé devra être formatée en FAT32 (voir le guide).";
        }

        public static ResultatCopie Copier(DriveInfo cle, string dossierSource, Action<int, string> progression)
        {
            long libre;
            try { libre = cle.AvailableFreeSpace; }
            catch (Exception ex)
            {
                Journal.Ecrire("Clé illisible : " + ex.Message);
                var r = new ResultatCopie();
                r.Message = "La clé USB ne répond pas. Retirez-la, rebranchez-la, puis réessayez.";
                return r;
            }
            return CopierVers(cle.RootDirectory.FullName, libre, dossierSource, MaxParDossier, progression);
        }

        /// <summary>Copie vers la racine donnée (séparé de DriveInfo pour pouvoir être testé sur un dossier ordinaire).</summary>
        public static ResultatCopie CopierVers(string racine, long espaceLibre, string dossierSource, int maxParDossier, Action<int, string> progression)
        {
            var resultat = new ResultatCopie();
            try
            {
                var chansons = Directory.Exists(dossierSource)
                    ? new DirectoryInfo(dossierSource).GetFiles("*.mp3").OrderBy(f => f.Name, StringComparer.CurrentCultureIgnoreCase).ToList()
                    : new List<FileInfo>();

                if (chansons.Count == 0)
                {
                    resultat.Message = "Il n'y a encore aucune chanson à copier. Téléchargez d'abord une chanson.";
                    return resultat;
                }

                // Dossiers « Musique », « Musique 2 », « Musique 3 »... déjà présents sur la clé.
                var dossiers = new List<string>();
                var dejaSurCle = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 1; ; i++)
                {
                    string d = Path.Combine(racine, i == 1 ? NomDossier : NomDossier + " " + i);
                    if (!Directory.Exists(d)) break;
                    dossiers.Add(d);
                    foreach (string f in Directory.GetFiles(d, "*.mp3"))
                        dejaSurCle.Add(Path.GetFileName(f));
                }

                var aCopier = chansons.Where(f => !dejaSurCle.Contains(f.Name)).ToList();
                resultat.DejaPresentes = chansons.Count - aCopier.Count;
                if (aCopier.Count == 0)
                {
                    resultat.Reussi = true;
                    resultat.Message = "Toutes vos chansons sont déjà sur la clé USB. Vous pouvez la retirer.";
                    return resultat;
                }

                long besoin = aCopier.Sum(f => f.Length) + 1024 * 1024;
                if (besoin > espaceLibre)
                {
                    resultat.Message = "Il n'y a pas assez de place sur la clé USB (il faut " + Taille(besoin)
                                     + ", il reste " + Taille(espaceLibre) + ").";
                    return resultat;
                }

                if (dossiers.Count == 0)
                    dossiers.Add(Path.Combine(racine, NomDossier));
                int index = dossiers.Count - 1;
                Directory.CreateDirectory(dossiers[index]);
                int dansDossier = Directory.GetFiles(dossiers[index]).Length;

                for (int n = 0; n < aCopier.Count; n++)
                {
                    if (dansDossier >= maxParDossier)
                    {
                        index++;
                        string nouveau = Path.Combine(racine, NomDossier + " " + (index + 1));
                        Directory.CreateDirectory(nouveau);
                        dossiers.Add(nouveau);
                        dansDossier = Directory.GetFiles(nouveau).Length;
                    }
                    FileInfo f = aCopier[n];
                    progression(n * 100 / aCopier.Count, "Copie sur la clé : " + (n + 1) + " de " + aCopier.Count + "  (" + Path.GetFileNameWithoutExtension(f.Name) + ")");
                    CopierSansCache(f.FullName, Path.Combine(dossiers[index], f.Name));
                    dansDossier++;
                    resultat.Copiees++;
                }

                progression(100, "Copie terminée.");
                resultat.Reussi = true;
                resultat.Message = (resultat.Copiees == 1 ? "1 chanson copiée" : resultat.Copiees + " chansons copiées")
                                 + " sur la clé USB, dans le dossier « " + NomDossier + " ». Vous pouvez retirer la clé et la brancher dans l'auto.";
                Journal.Ecrire("Copie sur " + racine + " : " + resultat.Copiees + " copiées, " + resultat.DejaPresentes + " déjà présentes");
                return resultat;
            }
            catch (Exception ex)
            {
                Journal.Ecrire("Erreur de copie : " + ex);
                resultat.Message = "La copie n'a pas fonctionné (la clé a peut-être été retirée ou elle est protégée en écriture). "
                                 + "Rebranchez la clé et réessayez.";
                return resultat;
            }
        }

        /// <summary>Copie en écriture directe, pour que la clé puisse être retirée dès la fin.</summary>
        static void CopierSansCache(string source, string destination)
        {
            string partiel = destination + ".partiel";
            using (var entree = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var sortie = new FileStream(partiel, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, FileOptions.WriteThrough))
            {
                entree.CopyTo(sortie, 1 << 16);
                sortie.Flush(true);
            }
            if (File.Exists(destination)) File.Delete(destination);
            File.Move(partiel, destination);
        }

        public static string Taille(long octets)
        {
            if (octets >= 1024L * 1024 * 1024) return (octets / (1024.0 * 1024 * 1024)).ToString("0.0") + " Go";
            if (octets >= 1024L * 1024) return (octets / (1024.0 * 1024)).ToString("0") + " Mo";
            return (octets / 1024.0).ToString("0") + " Ko";
        }
    }
}
