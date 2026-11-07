// Point d'entrée de « Musique pour l'auto ».

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Musique pour l'auto")]
[assembly: AssemblyDescription("Transforme une vidéo YouTube en MP3 compatible avec les radios d'automobile.")]
[assembly: AssemblyProduct("Musique pour l'auto")]
[assembly: AssemblyCopyright("Licence MIT")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]

namespace MusiquePourAuto
{
    static class PointEntree
    {
        [STAThread]
        static int Main(string[] args)
        {
            // Mode sans fenêtre, pour vérifier l'installation : MusiquePourAuto.exe --auto-test <lien>
            if (args.Length >= 2 && args[0] == "--auto-test")
                return AutoTest(args[1]);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e) { ErreurFatale(e.Exception); };
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e) { ErreurFatale(e.ExceptionObject as Exception); };

            bool nouvelle;
            using (var instance = new Mutex(true, "MusiquePourAuto-instance-unique", out nouvelle))
            {
                if (!nouvelle)
                {
                    MessageBox.Show("Musique pour l'auto est déjà ouvert. Regardez dans la barre des tâches, en bas de l'écran.",
                        "Musique pour l'auto", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return 0;
                }
                Journal.Ecrire("Démarrage, version " + Assembly.GetExecutingAssembly().GetName().Version);
                Application.Run(new FenetrePrincipale());
            }
            return 0;
        }

        static void ErreurFatale(Exception ex)
        {
            Journal.Ecrire("Erreur non gérée : " + ex);
            try
            {
                MessageBox.Show("Un problème inattendu est survenu. Fermez l'application et rouvrez-la.\n\n"
                              + "Détails enregistrés dans : " + Emplacements.Journal,
                    "Musique pour l'auto", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }

        static int AutoTest(string lien)
        {
            var rapport = new StringBuilder();
            Action<string> ecrire = delegate(string ligne)
            {
                rapport.AppendLine(ligne);
                try { Console.WriteLine(ligne); } catch { }
            };
            int code;
            try
            {
                string dernier = null;
                Action<int, string> progression = delegate(int p, string m)
                {
                    string ligne = (p < 0 ? "..." : p + "%") + " " + m;
                    if (ligne != dernier && (p < 0 || p % 10 == 0 || p == 100)) ecrire(ligne);
                    dernier = ligne;
                };
                if (!Outils.SontInstalles)
                {
                    ecrire("Installation des outils dans " + Emplacements.DossierOutils);
                    Outils.Installer(progression);
                }
                ResultatConversion r = Conversion.Convertir(lien, Emplacements.DossierChansons, progression);
                ecrire((r.Reussi ? "OK|" + r.Fichier : "ERREUR|") + "|" + r.Message);
                code = r.Reussi ? 0 : 1;
            }
            catch (Exception ex)
            {
                ecrire("ERREUR||" + ex);
                code = 2;
            }
            try
            {
                Directory.CreateDirectory(Emplacements.Accueil);
                File.WriteAllText(Path.Combine(Emplacements.Accueil, "auto-test.txt"), rapport.ToString(), Encoding.UTF8);
            }
            catch { }
            return code;
        }
    }
}
