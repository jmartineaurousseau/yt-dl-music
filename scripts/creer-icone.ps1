# Génère src\icone.ico (note de musique blanche sur fond vert) en plusieurs tailles.
# Usage : powershell -ExecutionPolicy Bypass -File scripts\creer-icone.ps1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

public static class CreateurIcone
{
    static Bitmap Dessiner(int s)
    {
        var bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            float r = s * 0.22f, m = s <= 16 ? 0f : s * 0.03f, w = s - 2 * m;
            using (var fond = new GraphicsPath())
            {
                fond.AddArc(m, m, r * 2, r * 2, 180, 90);
                fond.AddArc(m + w - r * 2, m, r * 2, r * 2, 270, 90);
                fond.AddArc(m + w - r * 2, m + w - r * 2, r * 2, r * 2, 0, 90);
                fond.AddArc(m, m + w - r * 2, r * 2, r * 2, 90, 90);
                fond.CloseFigure();
                using (var pinceau = new LinearGradientBrush(new PointF(0, 0), new PointF(0, s),
                           Color.FromArgb(67, 160, 71), Color.FromArgb(27, 94, 32)))
                    g.FillPath(pinceau, fond);
            }

            // Note de musique (\u266B) dessinée comme une forme, centrée et mise à l'échelle.
            using (var note = new GraphicsPath())
            using (var police = new FontFamily("Segoe UI Symbol"))
            {
                note.AddString("\u266B", police, (int)FontStyle.Regular, 100f, new PointF(0, 0), StringFormat.GenericTypographic);
                RectangleF b = note.GetBounds();
                float cible = s * 0.62f;
                float echelle = cible / Math.Max(b.Width, b.Height);
                using (var mat = new Matrix())
                {
                    mat.Translate(s / 2f, s / 2f);
                    mat.Scale(echelle, echelle);
                    mat.Translate(-(b.X + b.Width / 2f), -(b.Y + b.Height / 2f));
                    note.Transform(mat);
                }
                g.FillPath(Brushes.White, note);
            }
        }
        return bmp;
    }

    static byte[] EnDib(Bitmap bmp)
    {
        int s = bmp.Width;
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        w.Write(40); w.Write(s); w.Write(s * 2); w.Write((short)1); w.Write((short)32);
        w.Write(0); w.Write(0); w.Write(0); w.Write(0); w.Write(0); w.Write(0);
        for (int y = s - 1; y >= 0; y--)
            for (int x = 0; x < s; x++)
            {
                Color c = bmp.GetPixel(x, y);
                w.Write(c.B); w.Write(c.G); w.Write(c.R); w.Write(c.A);
            }
        int ligneMasque = ((s + 31) / 32) * 4;
        w.Write(new byte[ligneMasque * s]); // masque ET vide : la transparence vient du canal alpha
        return ms.ToArray();
    }

    public static void Creer(string chemin)
    {
        int[] tailles = { 16, 20, 24, 32, 40, 48, 64, 256 };
        var images = new byte[tailles.Length][];
        for (int i = 0; i < tailles.Length; i++)
        {
            using (var bmp = Dessiner(tailles[i]))
            {
                if (tailles[i] >= 256)
                {
                    var ms = new MemoryStream();
                    bmp.Save(ms, ImageFormat.Png);
                    images[i] = ms.ToArray();
                }
                else images[i] = EnDib(bmp);
            }
        }
        using (var f = new BinaryWriter(File.Create(chemin)))
        {
            f.Write((short)0); f.Write((short)1); f.Write((short)tailles.Length);
            int decalage = 6 + 16 * tailles.Length;
            for (int i = 0; i < tailles.Length; i++)
            {
                f.Write((byte)(tailles[i] >= 256 ? 0 : tailles[i]));
                f.Write((byte)(tailles[i] >= 256 ? 0 : tailles[i]));
                f.Write((byte)0); f.Write((byte)0);
                f.Write((short)1); f.Write((short)32);
                f.Write(images[i].Length); f.Write(decalage);
                decalage += images[i].Length;
            }
            foreach (var img in images) f.Write(img);
        }
        using (var apercu = Dessiner(256)) apercu.Save(Path.ChangeExtension(chemin, ".png"), ImageFormat.Png);
    }
}
'@
$racine = Split-Path -Parent $PSScriptRoot
[CreateurIcone]::Creer((Join-Path $racine 'src\icone.ico'))
Write-Host "Icône créée : src\icone.ico"
