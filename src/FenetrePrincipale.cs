// Fenêtre principale : trois étapes simples, de gros boutons, des messages clairs.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MusiquePourAuto
{
    public sealed class FenetrePrincipale : Form
    {
        const int EM_SETCUEBANNER = 0x1501;

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        static readonly Color Vert = Color.FromArgb(46, 125, 50);
        static readonly Color VertFonce = Color.FromArgb(27, 94, 32);
        static readonly Color Bleu = Color.FromArgb(21, 101, 192);
        static readonly Color BleuFonce = Color.FromArgb(13, 71, 161);
        static readonly Color GrisDesactive = Color.FromArgb(189, 189, 189);
        static readonly Color TexteNormal = Color.FromArgb(33, 33, 33);
        static readonly Color TexteDiscret = Color.FromArgb(97, 97, 97);
        static readonly Color TexteSucces = Color.FromArgb(27, 94, 32);
        static readonly Color TexteErreur = Color.FromArgb(183, 28, 28);

        readonly TextBox txtLien;
        readonly Button btnColler, btnTelecharger, btnCopier, btnOuvrir;
        readonly ProgressBar barre;
        readonly Label lblStatut, lblListe, lblDossier;
        readonly ListBox lstChansons;
        readonly LinkLabel lnkAide;
        readonly ToolTip infobulles = new ToolTip();

        bool occupe;
        string dernierLienTraite = "";

        public FenetrePrincipale()
        {
            Text = "Musique pour l'auto";
            Font = new Font("Segoe UI", 11F);
            BackColor = Color.White;
            ForeColor = TexteNormal;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Font;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            int h = Font.Height; // toutes les tailles suivent la police (donc la mise à l'échelle Windows)
            ClientSize = new Size(h * 34, h * 36);
            MinimumSize = new Size(h * 28, h * 30);

            var grille = new TableLayoutPanel();
            grille.Dock = DockStyle.Fill;
            grille.ColumnCount = 1;
            grille.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            grille.Padding = new Padding(h, h * 3 / 4, h, h / 2);
            grille.SuspendLayout();

            var lblTitre = Etiquette("Musique pour l'auto", new Font("Segoe UI", 20F, FontStyle.Bold), Vert);
            var lblSousTitre = Etiquette("Transformez une vidéo YouTube en chanson MP3 pour votre clé USB.", Font, TexteDiscret);
            lblSousTitre.Margin = new Padding(3, 0, 3, h);

            var lblEtape1 = Etiquette("1.  Dans YouTube, copiez le lien de la vidéo, puis cliquez sur « Coller le lien ».", new Font("Segoe UI", 11F, FontStyle.Bold), TexteNormal);

            txtLien = new TextBox();
            txtLien.Font = new Font("Segoe UI", 14F);
            txtLien.Anchor = AnchorStyles.Left | AnchorStyles.Right; // centrée verticalement à côté du bouton
            txtLien.Margin = new Padding(3, 6, 6, 3);
            txtLien.HandleCreated += delegate
            {
                // Texte gris affiché quand la case est vide.
                SendMessage(txtLien.Handle, EM_SETCUEBANNER, (IntPtr)1, "Le lien YouTube va ici");
            };
            txtLien.TextChanged += delegate { MettreAJourBoutons(); };
            infobulles.SetToolTip(txtLien, "Le lien ressemble à : https://www.youtube.com/watch?v=...");

            btnColler = Bouton("Coller le lien", Bleu, BleuFonce, h * 2 + 6);
            btnColler.AutoSize = true;
            btnColler.Padding = new Padding(h / 2, 0, h / 2, 0);
            btnColler.Click += delegate { CollerLien(true); };

            var ligneLien = new TableLayoutPanel();
            ligneLien.ColumnCount = 2;
            ligneLien.RowCount = 1;
            ligneLien.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            ligneLien.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            ligneLien.Dock = DockStyle.Fill;
            ligneLien.AutoSize = true;
            ligneLien.Margin = new Padding(0, 0, 0, h / 2);
            ligneLien.Controls.Add(txtLien, 0, 0);
            ligneLien.Controls.Add(btnColler, 1, 0);

            var lblEtape2 = Etiquette("2.  Cliquez sur le bouton vert et attendez quelques secondes.", new Font("Segoe UI", 11F, FontStyle.Bold), TexteNormal);

            btnTelecharger = Bouton("Télécharger la chanson en MP3", Vert, VertFonce, h * 3);
            btnTelecharger.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            btnTelecharger.Dock = DockStyle.Fill;
            btnTelecharger.Click += BtnTelecharger_Click;

            barre = new ProgressBar();
            barre.Dock = DockStyle.Fill;
            barre.Height = h;
            barre.Margin = new Padding(3, h / 2, 3, 3);
            barre.Style = ProgressBarStyle.Continuous;

            lblStatut = new Label();
            lblStatut.AutoSize = false;
            lblStatut.Dock = DockStyle.Fill;
            lblStatut.Font = new Font("Segoe UI", 12F);
            lblStatut.Height = lblStatut.Font.Height * 4 + 4;
            lblStatut.TextAlign = ContentAlignment.TopLeft;
            lblStatut.Margin = new Padding(3, 6, 3, h / 2);

            var lblEtape3 = Etiquette("3.  Branchez votre clé USB, puis copiez-y vos chansons.", new Font("Segoe UI", 11F, FontStyle.Bold), TexteNormal);

            btnCopier = Bouton("Copier sur la clé USB", Bleu, BleuFonce, h * 2 + h / 2);
            btnCopier.Dock = DockStyle.Fill;
            btnCopier.Click += BtnCopier_Click;

            btnOuvrir = Bouton("Ouvrir le dossier des chansons", Color.FromArgb(238, 238, 238), Color.FromArgb(224, 224, 224), h * 2 + h / 2);
            btnOuvrir.ForeColor = TexteNormal;
            btnOuvrir.Dock = DockStyle.Fill;
            btnOuvrir.Click += delegate { OuvrirDossier(null); };

            var ligneBoutons = new TableLayoutPanel();
            ligneBoutons.ColumnCount = 2;
            ligneBoutons.RowCount = 1;
            ligneBoutons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            ligneBoutons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            ligneBoutons.Dock = DockStyle.Fill;
            ligneBoutons.AutoSize = true;
            ligneBoutons.Margin = new Padding(0, 0, 0, h / 2);
            ligneBoutons.Controls.Add(btnCopier, 0, 0);
            ligneBoutons.Controls.Add(btnOuvrir, 1, 0);

            lblListe = Etiquette("Vos chansons :", new Font("Segoe UI", 10F, FontStyle.Bold), TexteDiscret);

            lstChansons = new ListBox();
            lstChansons.Dock = DockStyle.Fill;
            lstChansons.IntegralHeight = false;
            lstChansons.Font = new Font("Segoe UI", 11F);
            lstChansons.BorderStyle = BorderStyle.FixedSingle;
            lstChansons.DoubleClick += delegate { EcouterSelection(); };
            lstChansons.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Delete) SupprimerSelection();
                if (e.KeyCode == Keys.Enter) EcouterSelection();
            };
            lstChansons.MouseDown += delegate(object s, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Right) return;
                int i = lstChansons.IndexFromPoint(e.Location);
                if (i >= 0) lstChansons.SelectedIndex = i;
            };
            var menu = new ContextMenuStrip();
            menu.Font = Font;
            menu.Items.Add("Écouter", null, delegate { EcouterSelection(); });
            menu.Items.Add("Afficher dans le dossier", null, delegate
            {
                var c = Selection();
                if (c != null) OuvrirDossier(c.Chemin);
            });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Supprimer (mettre à la corbeille)", null, delegate { SupprimerSelection(); });
            menu.Opening += delegate(object s, System.ComponentModel.CancelEventArgs e) { e.Cancel = Selection() == null; };
            lstChansons.ContextMenuStrip = menu;
            infobulles.SetToolTip(lstChansons, "Double-cliquez sur une chanson pour l'écouter. Clic droit pour la supprimer.");

            lnkAide = new LinkLabel();
            lnkAide.Text = "Aide";
            lnkAide.AutoSize = true;
            lnkAide.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lnkAide.LinkClicked += delegate { AfficherAide(); };
            lnkAide.Margin = new Padding(3, 3, h, 3);

            lblDossier = Etiquette("", new Font("Segoe UI", 9F), TexteDiscret);
            lblDossier.AutoEllipsis = true;
            lblDossier.AutoSize = false;
            lblDossier.Dock = DockStyle.Fill;
            lblDossier.Height = h * 3 / 2;
            lblDossier.TextAlign = ContentAlignment.MiddleLeft;

            var piedDePage = new TableLayoutPanel();
            piedDePage.ColumnCount = 2;
            piedDePage.RowCount = 1;
            piedDePage.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            piedDePage.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            piedDePage.Dock = DockStyle.Fill;
            piedDePage.AutoSize = true;
            piedDePage.Controls.Add(lnkAide, 0, 0);
            piedDePage.Controls.Add(lblDossier, 1, 0);

            var lignes = new Control[]
            {
                lblTitre, lblSousTitre, lblEtape1, ligneLien, lblEtape2, btnTelecharger, barre, lblStatut,
                lblEtape3, ligneBoutons, lblListe, lstChansons, piedDePage
            };
            grille.RowCount = lignes.Length;
            for (int i = 0; i < lignes.Length; i++)
            {
                grille.RowStyles.Add(lignes[i] == lstChansons ? new RowStyle(SizeType.Percent, 100F) : new RowStyle(SizeType.AutoSize));
                grille.Controls.Add(lignes[i], 0, i);
            }
            grille.ResumeLayout(false);
            Controls.Add(grille);

            AcceptButton = btnTelecharger;
            Shown += FenetrePrincipale_Shown;
            Activated += delegate
            {
                if (!occupe)
                {
                    CollerLien(false);
                    RafraichirListe(null);
                }
            };
            FormClosing += FenetrePrincipale_FormClosing;

            lblDossier.Text = "Vos chansons sont enregistrées dans : " + Emplacements.DossierChansons;
            infobulles.SetToolTip(lblDossier, Emplacements.DossierChansons);
            AfficherStatut("Prêt. Collez un lien YouTube pour commencer.", TexteDiscret);
            MettreAJourBoutons();
        }

        // ---------- Démarrage : préparation des outils ----------

        async void FenetrePrincipale_Shown(object sender, EventArgs e)
        {
            RafraichirListe(null);
            if (!Outils.SontInstalles)
            {
                await PreparerOutils();
            }
            else if (Outils.MiseAJourDue())
            {
                // En arrière-plan : YouTube change souvent, yt-dlp doit rester à jour.
                var miseAJour = Task.Run(() => Outils.MettreAJourYtDlp());
            }
            CollerLien(false);
        }

        async Task PreparerOutils()
        {
            Occuper(true);
            AfficherStatut("Première utilisation : l'application télécharge ses outils (environ 150 Mo). "
                         + "Cela peut prendre quelques minutes. Ça ne se fera qu'une seule fois.", TexteNormal);
            barre.Style = ProgressBarStyle.Continuous;
            barre.Value = 0;
            string erreur = null;
            try
            {
                await Task.Run(() => Outils.Installer(Rapporteur()));
            }
            catch (Exception ex)
            {
                Journal.Ecrire("Préparation échouée : " + ex);
                erreur = ex is InvalidOperationException ? ex.Message
                    : "La préparation n'a pas fonctionné. Vérifiez la connexion Internet, puis cliquez sur le bouton vert pour réessayer.";
            }
            Occuper(false);
            if (erreur != null)
            {
                barre.Value = 0;
                AfficherStatut(erreur, TexteErreur);
            }
            else
            {
                barre.Value = 0;
                AfficherStatut("L'application est prête ! Collez un lien YouTube pour commencer.", TexteSucces);
            }
            MettreAJourBoutons();
        }

        // ---------- Étape 1 : coller le lien ----------

        void CollerLien(bool demandeParUtilisateur)
        {
            string texte = "";
            try { if (Clipboard.ContainsText()) texte = Clipboard.GetText().Trim(); } catch { }

            if (!demandeParUtilisateur)
            {
                // Remplissage automatique seulement si la case est vide et que c'est un nouveau lien YouTube.
                if (txtLien.TextLength == 0 && NomsChansons.EstUnLienYouTube(texte) && texte != dernierLienTraite)
                    txtLien.Text = texte;
                return;
            }

            if (texte.Length == 0)
            {
                AfficherStatut("Rien à coller : il faut d'abord copier le lien dans YouTube (bouton « Partager », puis « Copier »).", TexteErreur);
                return;
            }
            txtLien.Text = texte;
            if (NomsChansons.EstUnLienYouTube(texte))
            {
                AfficherStatut("Lien collé. Cliquez maintenant sur le bouton vert.", TexteNormal);
                btnTelecharger.Focus();
            }
            else
            {
                AfficherStatut("Ce que vous avez copié ne ressemble pas à un lien YouTube. Copiez le lien de la vidéo et réessayez.", TexteErreur);
            }
        }

        // ---------- Étape 2 : télécharger ----------

        async void BtnTelecharger_Click(object sender, EventArgs e)
        {
            if (occupe) return;
            if (!Outils.SontInstalles)
            {
                await PreparerOutils();
                if (!Outils.SontInstalles) return;
            }

            string lien = txtLien.Text.Trim();
            if (lien.Length == 0)
            {
                AfficherStatut("Collez d'abord le lien de la vidéo YouTube dans la case blanche.", TexteErreur);
                txtLien.Focus();
                return;
            }
            if (!NomsChansons.RessembleAUnLien(lien))
            {
                AfficherStatut("Ce texte n'est pas un lien. Dans YouTube, cliquez sur « Partager », puis « Copier », et collez le lien ici.", TexteErreur);
                return;
            }

            Occuper(true);
            AfficherStatut("Recherche de la vidéo...", TexteNormal);
            ResultatConversion r = await Task.Run(() => Conversion.Convertir(lien, Emplacements.DossierChansons, Rapporteur()));
            Occuper(false);

            barre.Style = ProgressBarStyle.Continuous;
            if (r.Reussi)
            {
                barre.Value = 100;
                dernierLienTraite = lien;
                txtLien.Clear();
                AfficherStatut(r.Message + "\nVous pouvez coller un autre lien, ou copier vos chansons sur la clé USB (étape 3).", TexteSucces);
                RafraichirListe(r.Fichier);
                System.Media.SystemSounds.Asterisk.Play();
            }
            else
            {
                barre.Value = 0;
                AfficherStatut(r.Message, TexteErreur);
            }
            MettreAJourBoutons();
        }

        // ---------- Étape 3 : copier sur la clé ----------

        async void BtnCopier_Click(object sender, EventArgs e)
        {
            if (occupe) return;
            List<DriveInfo> cles = CleUsb.Lister();
            if (cles.Count == 0)
            {
                AfficherStatut("Aucune clé USB trouvée. Branchez la clé dans l'ordinateur, attendez quelques secondes, puis cliquez de nouveau.", TexteErreur);
                return;
            }

            DriveInfo cle = cles[0];
            if (cles.Count > 1)
            {
                cle = ChoisirCle(cles);
                if (cle == null) return;
            }

            string avertissement = CleUsb.AvertissementFormat(cle);
            if (avertissement != null)
            {
                var choix = MessageBox.Show(this, avertissement + "\n\nVoulez-vous copier les chansons quand même ?",
                    "Format de la clé USB", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (choix != DialogResult.Yes) return;
            }

            Occuper(true);
            barre.Style = ProgressBarStyle.Continuous;
            barre.Value = 0;
            AfficherStatut("Copie sur la clé USB...", TexteNormal);
            ResultatCopie r = await Task.Run(() => CleUsb.Copier(cle, Emplacements.DossierChansons, Rapporteur()));
            Occuper(false);
            barre.Value = r.Reussi ? 100 : 0;
            AfficherStatut(r.Message, r.Reussi ? TexteSucces : TexteErreur);
            if (r.Reussi && r.Copiees > 0) System.Media.SystemSounds.Asterisk.Play();
            MettreAJourBoutons();
        }

        DriveInfo ChoisirCle(List<DriveInfo> cles)
        {
            using (var f = new Form())
            {
                int h = Font.Height;
                f.Text = "Quelle clé USB ?";
                f.Font = Font;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MinimizeBox = false;
                f.MaximizeBox = false;
                f.StartPosition = FormStartPosition.CenterParent;
                f.ClientSize = new Size(h * 26, h * 12);

                var lbl = new Label();
                lbl.Text = "Plusieurs clés USB sont branchées. Choisissez celle pour l'auto :";
                lbl.SetBounds(h, h / 2, h * 24, h * 2);
                var liste = new ListBox();
                liste.SetBounds(h, h * 5 / 2, h * 24, h * 6);
                liste.IntegralHeight = false;
                foreach (var c in cles) liste.Items.Add(CleUsb.Decrire(c));
                liste.SelectedIndex = 0;
                var ok = new Button();
                ok.Text = "Choisir";
                ok.DialogResult = DialogResult.OK;
                ok.SetBounds(h * 13, h * 9, h * 6, h * 2 + 4);
                var annuler = new Button();
                annuler.Text = "Annuler";
                annuler.DialogResult = DialogResult.Cancel;
                annuler.SetBounds(h * 19 + 6, h * 9, h * 6, h * 2 + 4);
                liste.DoubleClick += delegate { f.DialogResult = DialogResult.OK; };
                f.Controls.AddRange(new Control[] { lbl, liste, ok, annuler });
                f.AcceptButton = ok;
                f.CancelButton = annuler;
                return f.ShowDialog(this) == DialogResult.OK && liste.SelectedIndex >= 0 ? cles[liste.SelectedIndex] : null;
            }
        }

        // ---------- Liste des chansons ----------

        sealed class Chanson
        {
            public string Chemin;
            public override string ToString() { return Path.GetFileNameWithoutExtension(Chemin); }
        }

        void RafraichirListe(string aSelectionner)
        {
            try
            {
                string dossier = Emplacements.DossierChansons;
                var fichiers = Directory.Exists(dossier)
                    ? new DirectoryInfo(dossier).GetFiles("*.mp3").OrderByDescending(f => f.LastWriteTimeUtc).ToList()
                    : new List<FileInfo>();

                string selectionActuelle = aSelectionner;
                if (selectionActuelle == null && Selection() != null) selectionActuelle = Selection().Chemin;

                lstChansons.BeginUpdate();
                lstChansons.Items.Clear();
                foreach (var f in fichiers)
                {
                    var c = new Chanson();
                    c.Chemin = f.FullName;
                    int i = lstChansons.Items.Add(c);
                    if (selectionActuelle != null && string.Equals(f.FullName, selectionActuelle, StringComparison.OrdinalIgnoreCase))
                        lstChansons.SelectedIndex = i;
                }
                lstChansons.EndUpdate();

                if (fichiers.Count == 0)
                    lblListe.Text = "Vos chansons : aucune pour l'instant.";
                else
                    lblListe.Text = "Vos chansons (" + fichiers.Count + ") - double-cliquez pour écouter :";
            }
            catch (Exception ex)
            {
                Journal.Ecrire("Liste des chansons : " + ex.Message);
            }
        }

        Chanson Selection()
        {
            return lstChansons.SelectedItem as Chanson;
        }

        void EcouterSelection()
        {
            var c = Selection();
            if (c == null || !File.Exists(c.Chemin)) return;
            try { Process.Start(c.Chemin); }
            catch { AfficherStatut("Impossible d'ouvrir la chanson : aucun lecteur de musique n'est installé.", TexteErreur); }
        }

        void SupprimerSelection()
        {
            var c = Selection();
            if (c == null || occupe) return;
            var choix = MessageBox.Show(this, "Supprimer la chanson « " + c + " » de l'ordinateur ?\n\n(Elle sera mise dans la corbeille. Elle reste sur la clé USB si elle y a déjà été copiée.)",
                "Supprimer une chanson", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (choix != DialogResult.Yes) return;
            try
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(c.Chemin,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                AfficherStatut("La chanson « " + c + " » a été mise à la corbeille.", TexteDiscret);
            }
            catch (Exception ex)
            {
                Journal.Ecrire("Suppression : " + ex.Message);
                AfficherStatut("Impossible de supprimer la chanson (elle est peut-être en train de jouer).", TexteErreur);
            }
            RafraichirListe(null);
        }

        void OuvrirDossier(string fichierASelectionner)
        {
            try
            {
                Directory.CreateDirectory(Emplacements.DossierChansons);
                if (fichierASelectionner != null && File.Exists(fichierASelectionner))
                    Process.Start("explorer.exe", "/select,\"" + fichierASelectionner + "\"");
                else
                    Process.Start("explorer.exe", "\"" + Emplacements.DossierChansons + "\"");
            }
            catch (Exception ex)
            {
                Journal.Ecrire("Ouverture du dossier : " + ex.Message);
            }
        }

        void AfficherAide()
        {
            MessageBox.Show(this,
                "COMMENT FAIRE\n\n" +
                "1. Ouvrez YouTube et trouvez la chanson voulue.\n" +
                "2. Copiez le lien : sous la vidéo, cliquez sur « Partager », puis sur « Copier ».\n" +
                "    (Ou cliquez dans la barre d'adresse en haut, puis Ctrl+C.)\n" +
                "3. Revenez ici : cliquez sur « Coller le lien », puis sur le bouton vert.\n" +
                "4. Recommencez pour chaque chanson voulue.\n" +
                "5. Branchez la clé USB et cliquez sur « Copier sur la clé USB ».\n" +
                "6. Dans l'auto, branchez la clé dans le port USB et choisissez la source « USB » sur la radio.\n\n" +
                "BON À SAVOIR\n\n" +
                "• La clé USB doit être en format FAT32 (c'est le cas de la plupart des clés de 32 Go ou moins).\n" +
                "• Une seule chanson est téléchargée à la fois, même si le lien vient d'une liste de lecture.\n" +
                "• Vos chansons restent aussi sur l'ordinateur, dans :\n    " + Emplacements.DossierChansons + "\n" +
                "• En cas de problème, le fichier suivant aide à trouver la cause :\n    " + Emplacements.Journal,
                "Aide - Musique pour l'auto", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ---------- Utilitaires d'affichage ----------

        Action<int, string> Rapporteur()
        {
            return delegate(int pourcent, string message)
            {
                if (IsDisposed) return;
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        if (pourcent < 0)
                        {
                            if (barre.Style != ProgressBarStyle.Marquee) barre.Style = ProgressBarStyle.Marquee;
                        }
                        else
                        {
                            if (barre.Style != ProgressBarStyle.Continuous) barre.Style = ProgressBarStyle.Continuous;
                            barre.Value = Math.Max(0, Math.Min(100, pourcent));
                        }
                        AfficherStatut(message, TexteNormal);
                    });
                }
                catch (InvalidOperationException)
                {
                    // fenêtre en cours de fermeture
                }
            };
        }

        void AfficherStatut(string message, Color couleur)
        {
            lblStatut.Text = message;
            lblStatut.ForeColor = couleur;
        }

        void Occuper(bool valeur)
        {
            occupe = valeur;
            UseWaitCursor = valeur;
            if (valeur)
            {
                barre.Style = ProgressBarStyle.Marquee;
            }
            MettreAJourBoutons();
        }

        void MettreAJourBoutons()
        {
            txtLien.Enabled = !occupe;
            btnColler.Enabled = !occupe;
            btnCopier.Enabled = !occupe;
            btnTelecharger.Enabled = !occupe;
            btnTelecharger.Text = !Outils.SontInstalles && !occupe ? "Réessayer la préparation" : "Télécharger la chanson en MP3";
        }

        void FenetrePrincipale_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!occupe || e.CloseReason != CloseReason.UserClosing) return;
            var choix = MessageBox.Show(this, "Un travail est en cours. Voulez-vous vraiment fermer l'application ?",
                "Musique pour l'auto", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (choix != DialogResult.Yes)
                e.Cancel = true;
            else
                Programme.ArreterTout();
        }

        static Label Etiquette(string texte, Font police, Color couleur)
        {
            var l = new Label();
            l.Text = texte;
            l.Font = police;
            l.ForeColor = couleur;
            l.AutoSize = true;
            l.Margin = new Padding(3, 3, 3, 3);
            l.Dock = DockStyle.Fill; // dans la grille, le texte passe à la ligne au lieu d'être coupé
            return l;
        }

        static Button Bouton(string texte, Color fond, Color survol, int hauteur)
        {
            var b = new Button();
            b.Text = texte;
            b.Height = hauteur;
            b.MinimumSize = new Size(0, hauteur);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = survol;
            b.FlatAppearance.MouseDownBackColor = survol;
            b.BackColor = fond;
            b.ForeColor = Color.White;
            b.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
            b.UseVisualStyleBackColor = false;
            b.Margin = new Padding(3, 6, 3, 3);
            b.EnabledChanged += delegate
            {
                b.BackColor = b.Enabled ? fond : GrisDesactive;
            };
            return b;
        }
    }
}
