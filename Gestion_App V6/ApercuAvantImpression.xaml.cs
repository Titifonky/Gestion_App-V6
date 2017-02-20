using System;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using MapiApi;
using System.ComponentModel;
using LogDebugging;

namespace Gestion
{
    /// <summary>
    /// Logique d'interaction pour Window1.xaml
    /// </summary>
    public partial class ApercuAvantImpression : Window
    {
        private FlowDocument _Doc;
        private String _NomFichierSansExt;
        private DirectoryInfo _Dossier;
        private Boolean _EcraserFichier = false;
        private Utilisateur _Utilisateur;

        public ApercuAvantImpression(FlowDocument d, String nomFichierSansExt, DirectoryInfo dossier, Utilisateur utilisateur, Boolean ecraserFichier = false)
        {
            InitializeComponent();
            _Doc = d;
            _NomFichierSansExt = nomFichierSansExt.Flat().PrepareForFilename();
            _Dossier = dossier;
            _EcraserFichier = (ecraserFichier) ? ecraserFichier : utilisateur.EcraserIndicePDF;
            _Utilisateur = utilisateur;

            xUtilisateur.DataContext = _Utilisateur;

            xViewer.Document = _Doc;
            xViewer.ViewingMode = FlowDocumentReaderViewingMode.Scroll;

            this.Closing += new CancelEventHandler(MainWindow_Closing);

            WindowParam.AjouterParametre(this);
            WindowParam.RestaurerParametre(this);
        }

        private String NomFichier()
        {
            String Ext = ".pdf";
            String Chemin = Path.Combine(_Dossier.FullName, _NomFichierSansExt + " " + DateTime.Today.ToString("yyyy-MM-dd"));

            String NomComplet = Chemin + Ext;

            if (_EcraserFichier)
            {
                FileInfo[] Fi = _Dossier.GetFiles(_NomFichierSansExt + "*" + Ext);
                if ((Fi.LongLength > 0) && (MessageBox.Show(String.Format("Un fichier nommé \"{0}\" existe déjà\nVoulez vous vraiement le supprimer ?", _NomFichierSansExt + Ext), "Suppression", MessageBoxButton.OKCancel) == MessageBoxResult.OK))
                {
                    foreach (FileInfo F in Fi)
                        F.Delete();

                    return NomComplet;
                }
            }

            int i = 1;

            NomComplet = Chemin + "_" + i.ToString() + Ext;
            
            while (File.Exists(NomComplet))
            {
                i++;
                NomComplet = Chemin + "_" + i.ToString() + Ext;
            }

            return NomComplet;
        }

        private void Mail(FileInfo F)
        {
            try
            {
                Mapi mapi = new Mapi();

                mapi.AddAttachment(F.FullName);
                mapi.SendMailPopup("MFB : " + _NomFichierSansExt, "");
            }
            catch(Exception _Exception)
            {
                Log.Methode<ApercuAvantImpression>();
                Log.Message(_Exception);
            }
        }

        private void Imprimer()
        {
            DocumentPaginator Doc = MEP.Paginer(_Doc);

            // Create a XpsDocumentWriter object, implicitly opening a Windows common print dialog,
            // and allowing the user to select a printer.

            // get information about the dimensions of the seleted printer+media.
            PrintDocumentImageableArea ia = null;
            System.Windows.Xps.XpsDocumentWriter docWriter = PrintQueue.CreateXpsDocumentWriter(ref ia);

            if (docWriter != null && ia != null)
                docWriter.Write(Doc);
        }

        private FileInfo Exporter_PDF()
        {
            DocumentPaginator Doc = MEP.Paginer(_Doc);

            String Chemin = NomFichier();
            FileInfo F = null;

            if (PDF.Creer(Doc, Chemin) || File.Exists(Chemin))
                F = new FileInfo(Chemin);

            return F;
        }

        private void Mail_Click(object sender, RoutedEventArgs e)
        {
            FileInfo F = Exporter_PDF();

            if (F != null)
                Mail(F);

            this.Close();
        }

        private void Exporter_PDF_Click(object sender, RoutedEventArgs e)
        {
            FileInfo F = Exporter_PDF();

            if (F != null)
                System.Diagnostics.Process.Start(F.FullName);

            this.Close();
        }

        private void Imprimer_Click(object sender, RoutedEventArgs e)
        {
            Imprimer();

            if(_Utilisateur.CreerPDF)
                Exporter_PDF();

            this.Close();
        }

        private void ImprimerEtMail_Click(object sender, RoutedEventArgs e)
        {
            Imprimer();

            FileInfo F = Exporter_PDF();

            if (F != null)
                Mail(F);

            this.Close();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            WindowParam.SauverParametre(this);
        }
    }
}
