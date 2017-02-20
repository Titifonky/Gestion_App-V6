using System;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using MapiApi;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

namespace Gestion
{
    public partial class SelectionnerFichier : Window
    {
        private class FichierComparer : IComparer<Fichier>
        {

            [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            static extern int StrCmpLogicalW(String x, String y);

            public int Compare(Fichier x, Fichier y)
            {
                return StrCmpLogicalW(y.Nom, x.Nom);
            }

        }

        public class Fichier : INotifyPropertyChanged
        {
            private String _Nom = "";
            public String Nom
            {
                get { return _Nom; }
                set { Set(ref _Nom, value); }
            }

            private String _Chemin = "";
            public String Chemin
            {
                get { return _Chemin; }
                set { Set(ref _Chemin, value); }
            }

            public Fichier(FileInfo f)
            {
                Nom = Path.GetFileNameWithoutExtension(f.FullName);
                Chemin = f.FullName;
            }

            #region Notification WPF

            protected bool Set<U>(ref U field, U value, [CallerMemberName]string propertyName = "")
            {
                if (EqualityComparer<U>.Default.Equals(field, value)) { return false; }
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged([CallerMemberName] String NomProp = null)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(NomProp));
                }
            }

            #endregion

        }

        private ObservableCollection<Fichier> _ListeObsFichier = null;

        public SelectionnerFichier(FileInfo[] liste)
        {
            InitializeComponent();

            if (liste.Length == 0) return;
            List<FileInfo> ListeFi = new List<FileInfo>(liste);
            List<Fichier> ListeFichier = ListeFi.ConvertAll<Fichier>(f => { return new Fichier(f); });
            ListeFichier.Sort(new FichierComparer());

            _ListeObsFichier = new ObservableCollection<Fichier>(ListeFichier);

            if (ListeFichier.Count == 1)
            {
                System.Diagnostics.Process.Start(ListeFichier.Last().Chemin);
                return;
            }

            xListe.DataContext = _ListeObsFichier;
            xListe.SelectedIndex = 0;

            this.Closing += new CancelEventHandler(MainWindow_Closing);

            WindowParam.AjouterParametre(this);
            WindowParam.RestaurerParametre(this);

            this.Show();
        }

        private void Ouvrir_Click(object sender, RoutedEventArgs e)
        {
            Fichier F = xListe.SelectedItem as Fichier;

            if (F == null) return;

            System.Diagnostics.Process.Start(F.Chemin);
        }

        private void OuvrirEtFermer_Click(object sender, RoutedEventArgs e)
        {
            Ouvrir_Click(sender, e);

            this.Close();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            WindowParam.SauverParametre(this);
        }

        private void Supprimer_Fichier_Click(object sender, RoutedEventArgs e)
        {
            ListBox V; ObservableCollection<Fichier> Liste; List<Fichier> Ls; Fichier L;
            if (Info(sender as MenuItem, out V, out Liste, out Ls, out L))
            {
                foreach (Fichier iL in Ls)
                {
                    File.Delete(iL.Chemin);
                    Liste.Remove(iL);
                }
            }
        }

        private Boolean Info<T>(MenuItem I, out ListBox V, out ObservableCollection<T> Liste, out List<T> Ls, out T L)
            where T : INotifyPropertyChanged
        {
            V = null; Liste = null; Ls = null; L = default(T);
            if (I != null)
            {
                V = (I.Parent as ContextMenu).PlacementTarget as ListBox;
                Liste = V.ItemsSource as ObservableCollection<T>;
                Ls = V.SelectedItems.Cast<T>().ToList();
                L = (T)V.SelectedItem;

                if ((V != null) && (Liste != null) && (Ls != null) && ((L != null) || (Liste.Count == 0)))
                    return true;
            }
            return false;
        }
    }
}
