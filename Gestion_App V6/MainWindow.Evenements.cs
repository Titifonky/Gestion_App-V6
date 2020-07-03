using LogDebugging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Gestion
{

    public delegate void ModifierDevis(Client C, Devis DevisBase, ListBox Box);

    public partial class MainWindow : Window
    {
        private void SupprimerOnglet(object source, RoutedEventArgs args)
        {
            TabItem tabItem = args.Source as TabItem;
            if (tabItem != null)
            {
                FermerOnglet(tabItem.DataContext);

                xOnglets.Items.Remove(tabItem);
                if (DernierOngletActif != null)
                    xOnglets.SelectedItem = DernierOngletActif;

                return;
            }
        }

        private void SelectionOnglet(object sender, SelectionChangedEventArgs e)
        {
            //DernierOngletActif = xOnglets.SelectedItem as TabItem;
        }

        /// <summary>
        /// Valider les champs texte avec la touche entrée
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void TextBox_KeyEnterUpdate(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tBox = (TextBox)sender;
                DependencyProperty prop = TextBox.TextProperty;

                BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
                if (binding != null) { binding.UpdateSource(); }
            }
        }

        private void Editer_Menu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem I = sender as MenuItem;
            if (I != null)
            {
                Societe S = I.DataContext as Societe;
                if (S != null)
                {
                    FrameworkElement F = sender as FrameworkElement;
                    String Nom = F.Tag as String;

                    if (Nom == "Societe")
                    { EditerOnglet<Societe, Societe>(S); return; }

                    if (Nom == "Famille")
                    { EditerOnglet<Famille, Societe>(S); return; }

                    if (Nom == "Fournisseur")
                    { EditerOnglet<Fournisseur, Societe>(S); return; }

                    if (Nom == "Utilisateur")
                    { EditerOnglet<Utilisateur, Societe>(S); return; }
                }

            }
        }

        private void Editer_Click(object sender, RoutedEventArgs e)
        {
            MenuItem I = sender as MenuItem;
            if (I != null)
            {
                ListBox B = ((ContextMenu)I.Parent).PlacementTarget as ListBox;
                if (B != null)
                {
                    Client C = B.SelectedItem as Client;
                    if (C != null)
                    { EditerOnglet<Client>(C); return; }

                    Devis D = B.SelectedItem as Devis;
                    if (D != null)
                    { EditerOnglet<Devis>(D); return; }

                    Facture F = B.SelectedItem as Facture;
                    if (F != null)
                    {EditerOnglet<Facture>(F); return; }

                    Fournisseur Fr = B.SelectedItem as Fournisseur;
                    if (Fr != null)
                    {EditerOnglet<Fournisseur>(Fr); return; }

                    Utilisateur U = B.SelectedItem as Utilisateur;
                    if (U != null)
                    { EditerOnglet<Utilisateur>(U); return; }
                }
            }
        }

        private void Editer_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2)
            {
                Client C = ((FrameworkElement)sender).DataContext as Client;
                if (C != null)
                    EditerOnglet<Client>(C);

                Devis D = ((FrameworkElement)sender).DataContext as Devis;
                if (D != null)
                    EditerOnglet<Devis>(D);

                Facture F = ((FrameworkElement)sender).DataContext as Facture;
                if (F != null)
                    EditerOnglet<Facture>(F);

                Fournisseur Fr = ((FrameworkElement)sender).DataContext as Fournisseur;
                if (Fr != null)
                    EditerOnglet<Fournisseur>(Fr);

                Utilisateur U = ((FrameworkElement)sender).DataContext as Utilisateur;
                if (U != null)
                    EditerOnglet<Utilisateur>(U);
            }
        }

        private Boolean EditerOnglet<T>(T DataContext)
            where T : ObjetGestion
        {
            return EditerOnglet<T, T>(DataContext);
        }

        private Boolean EditerOnglet<T, U>(U DataContext)
            where T : ObjetGestion
            where U : ObjetGestion
        {
            String Titre = ""; String ModeleTitre = ""; String ModeleCorps = "";

            if (typeof(U) == typeof(Societe))
            {
                if (typeof(T) == typeof(Societe))
                { Titre = "Societe"; ModeleCorps = "xOngletSocieteControlTemplate"; }

                else if (typeof(T) == typeof(Famille))
                { Titre = "Famille"; ModeleCorps = "xOngletFamilleControlTemplate"; }

                else if (typeof(T) == typeof(Fournisseur))
                { Titre = "Fournisseur"; ModeleCorps = "xOngletFournisseurControlTemplate"; }

                else if (typeof(T) == typeof(Utilisateur))
                { Titre = "Utilisateur"; ModeleCorps = "xOngletUtilisateurControlTemplate"; }
            }
            else if (typeof(T) == typeof(Client))
            { ModeleTitre = "xTitreClient"; ModeleCorps = "xEditerClientControlTemplate"; }

            else if (typeof(T) == typeof(Devis))
            {
                ModeleTitre = "xTitreDevis"; ModeleCorps = "xEditerDevisControlTemplate";
                U devis = (U)DataContext;

                var result1 = Bdd2.PreCharger(typeof(U), new List<ObjetGestion>() { devis });

                var result2 = Bdd2.PreCharger(typeof(Poste), result1[typeof(Poste)]);

                var result3 = Bdd2.PreCharger(typeof(Ligne_Poste), result2[typeof(Ligne_Poste)]);
            }

            else if (typeof(T) == typeof(Facture))
            { ModeleTitre = "xTitreFacture"; ModeleCorps = "xEditerFactureControlTemplate"; }

            else if (typeof(T) == typeof(Fournisseur))
            { ModeleTitre = "xTitreFournisseur"; ModeleCorps = "xEditerFournisseurControlTemplate"; }

            else if (typeof(T) == typeof(Utilisateur))
            { ModeleTitre = "xTitreUtilisateur"; ModeleCorps = "xEditerUtilisateurControlTemplate"; }

            if (DataContext != null)
            {

                OngletSupprimable Onglet = null;

                foreach (TabItem pTab in xOnglets.Items)
                {
                    if (pTab.DataContext == (object)DataContext)
                    {
                        Onglet = pTab as OngletSupprimable;
                        if (Onglet == null)
                            continue;
                    }
                }

                if (Onglet == null)
                {

                    Onglet = new OngletSupprimable();
                    if (String.IsNullOrWhiteSpace(Titre))
                    {
                        Onglet.Header = DataContext;
                        Onglet.HeaderTemplate = (DataTemplate)this.Resources[ModeleTitre];
                    }
                    else
                    {
                        Onglet.Header = Titre;
                    }

                    ContentControl Control = new ContentControl();
                    Control.Template = (ControlTemplate)this.Resources[ModeleCorps];
                    Onglet.Content = Control;
                    xOnglets.Items.Add(Onglet);
                    Onglet.DataContext = DataContext;
                }
                DernierOngletActif = xOnglets.SelectedItem as TabItem;
                xOnglets.SelectedItem = Onglet;
                return true;
            }

            return false;
        }

        private void FermerOnglet(Object DataContext)
        {
            if (DataContext is Client C)
            { C.CreerDossier(true); return; }

            if (DataContext is Devis D)
            { D.CreerDossier(false); return; }
        }

        private void Ouvrir_Dossier_Click(object sender, RoutedEventArgs e)
        {
            MenuItem I = sender as MenuItem;
            if (I != null)
            {
                DirectoryInfo Dir = null;
                ListBox B = ((ContextMenu)I.Parent).PlacementTarget as ListBox;
                if (B != null)
                {
                    Client C = B.SelectedItem as Client;
                    if (C != null)
                        Dir = C.Dossier;

                    Devis D = B.SelectedItem as Devis;
                    if (D != null)
                        Dir = D.Dossier;

                    Facture F = B.SelectedItem as Facture;
                    if (F != null)
                        Dir = F.Devis.Dossier;
                }
                else
                {
                    Grid Onglet = (I.Parent as ContextMenu).PlacementTarget as Grid;

                    if (Onglet != null)
                    {
                        Devis D = Onglet.DataContext as Devis;

                        if (D != null)
                            Dir = D.Dossier;
                    }
                }

                if (Dir != null)
                    System.Diagnostics.Process.Start(Dir.FullName);
            }
        }

        private void Ouvrir_Indice_Click(object sender, RoutedEventArgs e)
        {
            MenuItem I = sender as MenuItem;
            if (I != null)
            {
                DirectoryInfo Dir = null;
                ListBox B = ((ContextMenu)I.Parent).PlacementTarget as ListBox;
                if (B != null)
                {
                    Devis D = B.SelectedItem as Devis;
                    if (D != null)
                        Dir = D.DossierIndice;
                }
                else
                {
                    Grid Onglet = (I.Parent as ContextMenu).PlacementTarget as Grid;

                    if (Onglet != null)
                    {
                        Devis D = Onglet.DataContext as Devis;

                        if (D != null)
                            Dir = D.DossierIndice;
                    }
                }

                if (Dir != null)
                    System.Diagnostics.Process.Start(Dir.FullName);
            }
        }

        private void Enregistrer_Click(object sender, RoutedEventArgs e)
        {
            if (Bdd2.DoitEtreEnregistre)
            {
                Bdd2.Enregistrer();
                xDerniereSvg.Text = "Dernière sauvegarde à " + DateTime.Now.Hour + "h" + DateTime.Now.Minute + ":" + DateTime.Now.Second;
            }
            else
                xDerniereSvg.Text = "Base de donnée à jour";
        }

        private void Tout_Calculer_Click(object sender, RoutedEventArgs e)
        {

            if (MessageBox.Show("Voulez vous tout calculer ?", "Calcul", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel) return;

            String pInfo = "Calcul en cours ...";
            String pTitre = this.Title;
            this.Title = pInfo;

            Nettoyer(true);

            this.Title = pTitre;
        }

        private void Nettoyer(Boolean Calculer = true)
        {
            ListeObservable<Devis> ListeDevis = Bdd2.Liste<Devis>();
            ListeObservable<Poste> ListePoste = Bdd2.Liste<Poste>();
            ListeObservable<Ligne_Poste> ListeLigne_Poste = Bdd2.Liste<Ligne_Poste>();
            ListeObservable<Achat> ListeAchat = Bdd2.Liste<Achat>();

            ListeObservable<Facture> ListeFacture = Bdd2.Liste<Facture>();
            ListeObservable<Ligne_Facture> ListeLigne_Facture = Bdd2.Liste<Ligne_Facture>();

            String Titre = "Calcul des lignes de factures : ";
            int i = 1;
            int tt = ListeLigne_Facture.Count();

            foreach (Ligne_Facture Ligne_Facture in ListeLigne_Facture)
            {
                this.Title = Titre + " " + i.ToString() + "/" + tt.ToString();

                if (Ligne_Facture.Facture == null)
                    Ligne_Facture.Supprimer();
                else if (Calculer)
                    Ligne_Facture.Calculer(false);

                i++;
            }

            Titre = "Calcul des factures : ";
            i = 1;
            tt = ListeFacture.Count();
            foreach (Facture Facture in ListeFacture)
            {
                this.Title = Titre + " " + i.ToString() + "/" + tt.ToString();

                if ((Facture.Devis == null) || (Facture.ListeLigneFacture.Count == 0))
                    Facture.Supprimer();
                else if (Calculer)
                    Facture.Calculer(false);

                i++;
            }

            Titre = "Calcul des lignes de poste : ";
            i = 1;
            tt = ListeLigne_Poste.Count();
            foreach (Ligne_Poste Ligne_Poste in ListeLigne_Poste)
            {
                this.Title = Titre + " " + i.ToString() + "/" + tt.ToString();

                if (Ligne_Poste.Poste == null)
                    Ligne_Poste.Supprimer();
                else if (Calculer)
                    Ligne_Poste.Calculer(false);

                i++;
            }

            Titre = "Calcul des postes : ";
            i = 1;
            tt = ListePoste.Count();
            foreach (Poste Poste in ListePoste)
            {
                this.Title = Titre + " " + i.ToString() + "/" + tt.ToString();

                if (Poste.Devis == null)
                    Poste.Supprimer();
                else if (Calculer)
                    Poste.Calculer(false);

                i++;
            }

            Titre = "Calcul des devis : ";
            i = 1;
            tt = ListeDevis.Count();
            foreach (Devis Devis in ListeDevis)
            {
                this.Title = Titre + " " + i.ToString() + "/" + tt.ToString();

                if (Devis.Client == null)
                    Devis.Supprimer();
                else if (Calculer)
                    Devis.Calculer();

                i++;
            }

            Titre = "Calcul des achats : ";
            i = 1;
            tt = ListeAchat.Count();
            foreach (Achat Achat in ListeAchat)
            {
                this.Title = Titre + " " + i.ToString() + "/" + tt.ToString();

                if (Achat.Devis == null)
                    Achat.Supprimer();
                else if (Calculer)
                    Achat.Calculer();

                i++;
            }

        }

        private BindingExpression DevisExpItem = null;
        private BindingExpression FactureExpItem = null;
        private void Afficher_Tout_Les_Devis_Click(object sender, RoutedEventArgs e)
        {
            Svg_Binding();

            ToggleButton Bt = sender as ToggleButton;

            if (Bt.IsChecked == true)
                xListeDevis.ItemsSource = Bdd2.Liste<Devis>();
            else if (Bt.IsChecked == false)
                xListeDevis.SetBinding(ListBox.ItemsSourceProperty, DevisExpItem.ParentBindingBase);
        }

        private void Svg_Binding()
        {
            if(FactureExpItem == null)
                FactureExpItem = xListeFactureClient.GetBindingExpression(ListBox.ItemsSourceProperty);

            if (DevisExpItem == null)
                DevisExpItem = xListeDevis.GetBindingExpression(ListBox.ItemsSourceProperty);
        }

        private void Afficher_Toutes_Les_Factures_Click(object sender, RoutedEventArgs e)
        {
            Svg_Binding();

            ToggleButton Bt = sender as ToggleButton;

            if (Bt.IsChecked == true)
                xListeFactureClient.ItemsSource = Bdd2.Liste<Facture>();
            else if (Bt.IsChecked == false)
                xListeFactureClient.SetBinding(ListBox.ItemsSourceProperty, FactureExpItem.ParentBindingBase);
        }

        private void EffacerTextBox_Click(object sender, RoutedEventArgs e)
        {
            Button B = sender as Button;
            if (B == null) return;
            TextBox T = B.DataContext as TextBox;
            if (B == null) return;

            T.Text = "";

            DependencyProperty prop = TextBox.TextProperty;
            BindingExpression binding = BindingOperations.GetBindingExpression(T, prop);
            if (binding != null) { binding.UpdateSource(); }
        }

        #region EVENEMENT IMPRIMER

        private void Apercu_Devis_Click(object sender, RoutedEventArgs e)
        {
            Devis D = null;

            MenuItem M = sender as MenuItem;
            Grid Grid = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget as Grid;
            if (Grid != null)
                D = Grid.DataContext as Devis;
            else
            {
                ListBox V; ListeObservable<Devis> Liste; List<Devis> Ls; Devis L;
                if (Info(sender as MenuItem, out V, out Liste, out Ls, out L))
                    D = L;
            }

            DirectoryInfo pDossier = D.Dossier;
            if (pDossier == null)
                pDossier = D.CreerDossier(true);

            ApercuAvantImpression Fenetre = new ApercuAvantImpression(D.Impression(),
                                                                        D.Ref + " " + D.Description,
                                                                        pDossier,
                                                                        D.Client.Societe.UtilisateurCourant);
            Fenetre.Show();
        }

        private void Apercu_Facture_Click(object sender, RoutedEventArgs e)
        {
            Facture F = null;

            MenuItem M = sender as MenuItem;
            Grid Grid = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget as Grid;
            if (Grid != null)
                F = Grid.DataContext as Facture;
            else
            {
                ListBox V; ListeObservable<Facture> Liste; List<Facture> Ls; Facture L;
                if (Info(sender as MenuItem, out V, out Liste, out Ls, out L))
                    F = L;
            }

            DirectoryInfo pDossier = F.Devis.Dossier;
            if (pDossier == null)
                pDossier = F.Devis.CreerDossier(true);

            ApercuAvantImpression Fenetre = new ApercuAvantImpression(F.Impression(),
                                                                        F.Ref + " " + F.Devis.Description,
                                                                        pDossier,
                                                                        F.Devis.Client.Societe.UtilisateurCourant,
                                                                        true);
            Fenetre.Show();
        }

        private void Ouvrir_Devis_Click(object sender, RoutedEventArgs e)
        {
            Devis D = null;

            MenuItem M = sender as MenuItem;
            Grid Grid = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget as Grid;
            if (Grid != null)
                D = Grid.DataContext as Devis;
            else
            {
                ListBox V; ListeObservable<Devis> Liste; List<Devis> Ls; Devis L;
                if (Info(sender as MenuItem, out V, out Liste, out Ls, out L))
                    D = L;
            }

            DirectoryInfo pDossier = D.Dossier;
            if (pDossier == null) return;

            new SelectionnerFichier(pDossier.GetFiles(D.Ref + " *.pdf"));
        }

        private void Ouvrir_Facture_Click(object sender, RoutedEventArgs e)
        {
            Facture F = null;

            MenuItem M = sender as MenuItem;
            Grid Grid = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget as Grid;
            if (Grid != null)
                F = Grid.DataContext as Facture;
            else
            {
                ListBox V; ListeObservable<Facture> Liste; List<Facture> Ls; Facture L;
                if (Info(sender as MenuItem, out V, out Liste, out Ls, out L))
                    F = L;
            }

            DirectoryInfo pDossier = F.Devis.Dossier;
            if (pDossier == null) return;

            new SelectionnerFichier(pDossier.GetFiles(F.Ref + " *.pdf"));
        }


        #endregion

        #region EVENEMENT CLIENT

        private void Ajouter_Client_Click(object sender, RoutedEventArgs e)
        {
            ListeObservable<Client> Liste = Ajouter_List<Client, Societe>(sender, e);
            foreach(Client C in Liste)
            {
                Adresse_Client A= new Adresse_Client(C);
                EditerOnglet<Client>(C);
            }
        }

        private void Supprimer_Client_Click(object sender, RoutedEventArgs e)
        {
            Supprimer_List<Client>(sender, e, true);
        }

        #endregion

        #region EVENEMENT FOURNISSEUR

        private void Ajouter_Fournisseur_Click(object sender, RoutedEventArgs e)
        {
            Ajouter_List<Fournisseur, Societe>(sender, e);
        }

        private void Supprimer_Fournisseur_Click(object sender, RoutedEventArgs e)
        {
            Supprimer_List<Fournisseur>(sender, e, true);
        }

        #endregion

        #region EVENEMENT UTILISATEUR

        private void Ajouter_Utilisateur_Click(object sender, RoutedEventArgs e)
        {
            ListeObservable<Utilisateur> Liste = Ajouter_List<Utilisateur, Societe>(sender, e);
            foreach (Utilisateur U in Liste)
            {
                EditerOnglet<Utilisateur>(U);
            }
        }

        private void Supprimer_Utilisateur_Click(object sender, RoutedEventArgs e)
        {
            Supprimer_List<Utilisateur>(sender, e, true, true);
        }

        #endregion

        #region EVENEMENT DEVIS

        private void Ajouter_Devis_Click(object sender, RoutedEventArgs e)
        {
            ListeObservable<Devis> Liste = Ajouter_List<Devis, Client>(sender, e);
            foreach (Devis D in Liste)
            {
                Poste P = new Poste(D);
                Ligne_Poste L = new Ligne_Poste(P);
                EditerOnglet<Devis>(D);
            }
        }

        private void Ajouter_Devis_Indice_Click(object sender, RoutedEventArgs e)
        {
            ListeObservable<Devis> pListe = Ajouter_List<Devis, Client>(sender, e);
            if (pListe.Count == 0) return;

            ListBox V; ListeObservable<Devis> Liste; List<Devis> Ls; Devis L;
            if (Info(sender as MenuItem, out V, out Liste, out Ls, out L))
            {
                Devis pD = pListe[0];
                pD.CopierAvecIndice(L);

                L.Statut = StatutDevis_e.cIndice;

                EditerOnglet<Devis>(pD);
            }
        }

        private void Copier_Devis_Vers_Client_Click(object sender, RoutedEventArgs e)
        {
            Modifier_Devis(sender, e, Copier_Devis_Vers_Client);
        }

        private void Deplacer_Devis_Vers_Client_Click(object sender, RoutedEventArgs e)
        {
            Modifier_Devis(sender, e, Deplacer_Devis_Vers_Client);
        }

        private void Modifier_Devis(object sender, RoutedEventArgs e, ModifierDevis Dlgt)
        {
            ListBox V; ListeObservable<Devis> Liste; List<Devis> Ls; Devis L;
            if (Info(sender as MenuItem, out V, out Liste, out Ls, out L))
            {
                SelectionnerClient Fenetre = new SelectionnerClient(L, xListeClient, Dlgt);
                Fenetre.Show();

                Fenetre.Left = System.Windows.Forms.Control.MousePosition.X;
                Fenetre.Top = System.Windows.Forms.Control.MousePosition.Y;
            }
        }

        private void Deplacer_Devis_Vers_Client(Client C, Devis DevisBase, ListBox Box)
        {
            DirectoryInfo Dossier = DevisBase.DossierIndice;
            if(Dossier == null) Dossier = DevisBase.Dossier;

            DevisBase.Client = C;

            DevisBase.CreerDossier(true);

            try
            {
                if (Dossier != null)
                {
                    String NomDossier = DevisBase.DossierIndice.FullName;

                    foreach (FileInfo F in DevisBase.DossierIndice.GetFiles())
                    {
                        String Chemin = Path.Combine(Dossier.FullName, Path.GetFileName(F.FullName));
                        if (File.Exists(Chemin))
                        {
                            File.Delete(Chemin);
                            F.MoveTo(Chemin);
                        }
                    }

                    DevisBase.DossierIndice.Delete(true);

                    Dossier.MoveTo(NomDossier);
                }
            }
            catch { }

            Box.SelectedItem = C;

            EditerOnglet<Devis>(DevisBase);
        }

        private void Copier_Devis_Vers_Client(Client C, Devis DevisBase, ListBox Box)
        {
            Devis D = new Devis(C);

            D.Copier(DevisBase);

            Box.SelectedItem = C;

            EditerOnglet<Devis>(D);
        }

        private void Fusionner_Devis_Click(object sender, RoutedEventArgs e)
        {
            ListBox V; ListeObservable<Devis> Liste; List<Devis> Ls; Devis L;
            if (Info(sender as MenuItem, out V, out Liste, out Ls, out L))
            {
                Devis DevisBase = L;

                foreach (Devis D in Ls)
                {
                    if (DevisBase != D)
                        DevisBase.Importer(D);
                }

                DevisBase.ListePoste.Numeroter();

                EditerOnglet<Devis>(DevisBase);
            }
        }

        private void Fusionner_Nouveau_Devis_Click(object sender, RoutedEventArgs e)
        {
            ListBox V; ListeObservable<Devis> Liste; List<Devis> Ls; Devis L;
            if (Info(sender as MenuItem, out V, out Liste, out Ls, out L))
            {
                Devis DevisBase = Ajouter_List<Devis, Client>(sender, e, true).First();

                foreach (Devis D in Ls)
                {
                    DevisBase.Importer(D);
                    D.Statut = StatutDevis_e.cIndice;
                    if(!String.IsNullOrWhiteSpace(D.Description))
                        DevisBase.Description = (DevisBase.Description.Flat() + Environment.NewLine + D.Description.Flat()).Trim();
                }

                DevisBase.ListePoste.Numeroter();

                EditerOnglet<Devis>(DevisBase);
            }
        }

        private void Supprimer_Devis_Click(object sender, RoutedEventArgs e)
        {
            Supprimer_List<Devis>(sender, e, true);
        }

        #endregion

        #region EVENEMENT FACTURE

        private void Ajouter_Facture_Click(object sender, RoutedEventArgs e)
        {
            ListeObservable<Facture> Liste = Ajouter_List<Facture, Devis>(sender, e);
            foreach (Facture F in Liste)
            {
                EditerOnglet<Facture>(F);
            }
        }

        private void Supprimer_Facture_Click(object sender, RoutedEventArgs e)
        {
            Supprimer_List<Facture>(sender, e, true);
        }

        private void MajFacture_Click(object sender, RoutedEventArgs e)
        {
            Facture F = null;

            MenuItem M = sender as MenuItem;
            Grid Grid = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget as Grid;
            if (Grid != null)
                F = Grid.DataContext as Facture;
            else
            {
                ListBox V; ListeObservable<Facture> Liste; List<Facture> Ls; Facture L;
                if (Info(sender as MenuItem, out V, out Liste, out Ls, out L))
                    F = L;
            }

            F.MajLigne_Facture();
            
        }

        #endregion

        #region EVENEMENT POSTE MENU LIST

        private void Ajouter_Poste_Click(object sender, RoutedEventArgs e)
        {
            Ajouter_List<Poste, Devis>(sender, e);
        }

        private void Inserer_Poste_Click(object sender, RoutedEventArgs e)
        {
            Inserer_List<Poste, Devis>(sender, e);
        }

        private void Monter_Poste_Click(object sender, RoutedEventArgs e)
        {
            Monter_List<Poste>(sender, e);
        }

        private void Descendre_Poste_Click(object sender, RoutedEventArgs e)
        {
            Descendre_List<Poste>(sender, e);
        }

        private void Supprimer_Poste_Click(object sender, RoutedEventArgs e)
        {
            Supprimer_List<Poste>(sender, e);
        }

        private void Copier_Poste_Click(object sender, RoutedEventArgs e)
        {
            Copier_List<Poste>(sender, e);
        }

        private void Coller_Poste_Click(object sender, RoutedEventArgs e)
        {
            Coller_List<Poste>(sender, e);
        }

        private void Inserer_Coller_Poste_Click(object sender, RoutedEventArgs e)
        {
            Inserer_Coller_List<Poste, Devis>(sender, e);
        }

        #endregion

        #region EVENEMENT LIGNE_POSTE MENU LIST

        private void Ajouter_Ligne_Poste_Click(object sender, RoutedEventArgs e)
        {
            Ajouter_List<Ligne_Poste, Poste>(sender, e);
        }

        private void Inserer_Ligne_Poste_Click(object sender, RoutedEventArgs e)
        {
            Inserer_List<Ligne_Poste, Poste>(sender, e);
        }

        private void Monter_Ligne_Poste_Click(object sender, RoutedEventArgs e)
        {
            Monter_List<Ligne_Poste>(sender, e);
        }

        private void Descendre_Ligne_Poste_Click(object sender, RoutedEventArgs e)
        {
            Descendre_List<Ligne_Poste>(sender, e);
        }

        private void Supprimer_Ligne_Poste_Click(object sender, RoutedEventArgs e)
        {
            Supprimer_List<Ligne_Poste>(sender, e);
        }

        private void Copier_Ligne_Poste_Click(object sender, RoutedEventArgs e)
        {
            Copier_List<Ligne_Poste>(sender, e);
        }

        private void Coller_Ligne_Poste_Click(object sender, RoutedEventArgs e)
        {
            Coller_List<Ligne_Poste>(sender, e);
        }

        private void Inserer_Coller_Ligne_Poste_Click(object sender, RoutedEventArgs e)
        {
            Inserer_Coller_List<Ligne_Poste, Poste>(sender, e);
        }

        #endregion

        #region EVENEMENT FAMILLE MENU LIST

        private void Ajouter_Famille_Click(object sender, RoutedEventArgs e)
        {
            Ajouter_List<Famille, Societe>(sender, e);
        }

        private void Inserer_Famille_Click(object sender, RoutedEventArgs e)
        {
            Inserer_List<Famille, Societe>(sender, e);
        }

        private void Monter_Famille_Click(object sender, RoutedEventArgs e)
        {
            Monter_List<Famille>(sender, e);
        }

        private void Descendre_Famille_Click(object sender, RoutedEventArgs e)
        {
            Descendre_List<Famille>(sender, e);
        }

        private void Supprimer_Famille_Click(object sender, RoutedEventArgs e)
        {
            Supprimer_List<Famille>(sender, e);
        }

        private void Copier_Famille_Click(object sender, RoutedEventArgs e)
        {
            Copier_List<Famille>(sender, e);
        }

        private void Coller_Famille_Click(object sender, RoutedEventArgs e)
        {
            Coller_List<Famille>(sender, e);
        }

        private void Inserer_Coller_Famille_Click(object sender, RoutedEventArgs e)
        {
            Inserer_Coller_List<Famille, Societe>(sender, e);
        }

        #endregion

        #region EVENEMENT COMMANDE MENU LIST

        private void Ajouter_Commande_Click(object sender, RoutedEventArgs e)
        {
            Ajouter_List<Achat, Devis>(sender, e);
        }

        private void Supprimer_Commande_Click(object sender, RoutedEventArgs e)
        {
            Supprimer_List<Achat>(sender, e);
        }

        #endregion

        #region EVENEMENT ADRESSE_CLIENT MENU LIST

        private void Ajouter_Adresse_Client_Click(object sender, RoutedEventArgs e)
        {
            Ajouter_List<Adresse_Client, Client>(sender, e);
        }

        private void Monter_Adresse_Client_Click(object sender, RoutedEventArgs e)
        {
            Monter_List<Adresse_Client>(sender, e);
        }

        private void Descendre_Adresse_Client_Click(object sender, RoutedEventArgs e)
        {
            Descendre_List<Adresse_Client>(sender, e);
        }

        private void Supprimer_Adresse_Client_Click(object sender, RoutedEventArgs e)
        {
            Supprimer_List<Adresse_Client>(sender, e, true, true);
        }

        #endregion

        #region EVENEMENT MENU LIST

        Object _Copie_Liste;

        private Boolean Info<T>(MenuItem I, out ListBox V, out ListeObservable<T> Liste, out List<T> Ls, out T L)
            where T : INotifyPropertyChanged
        {
            V = null; Liste = null; Ls = null; L = default(T);
            if (I != null)
            {
                V = (I.Parent as ContextMenu).PlacementTarget as ListBox;
                Liste = V.ItemsSource as ListeObservable<T>;
                Ls = V.SelectedItems.Cast<T>().ToList();
                L = (T)V.SelectedItem;

                if ((V != null) && (Liste != null) && (Ls != null) && ((L != null) || (Liste.Count == 0)))
                    return true;
            }
            return false;
        }

        private T Ajouter<T>()
            where T : ObjetGestion, new()
        {
            return new T();
        }

        private T Ajouter<T, U>(U Parent)
            where T : ObjetGestion, new()
            where U : ObjetGestion
        {
            ConstructorInfo classConstructor = typeof(T).GetConstructor(new Type[] { typeof(U) });
            return (T)classConstructor.Invoke(new object[] { Parent });
        }

        private ListeObservable<T> Ajouter_List<T>(object sender, RoutedEventArgs e, Boolean UnSeul = false)
            where T : ObjetGestion, new()
        {
            ListeObservable<T> pListe = new ListeObservable<T>();

            ListBox V; ListeObservable<T> Liste; List<T> Ls; T L;
            if (Info(sender as MenuItem, out V, out Liste, out Ls, out L))
            {
                int Nb = Math.Max(Ls.Count, 1);
                if (UnSeul)
                    Nb = 1;

                // On force à faire au minimum un tour de boucle dans le cas ou la liste est vide.
                for (int i = 0; i < Nb; i++)
                    pListe.Add(Ajouter<T>());
            }

            return pListe;
        }

        private ListeObservable<T> Ajouter_List<T, U>(object sender, RoutedEventArgs e, Boolean UnSeul = false)
            where T : ObjetGestion, new()
            where U : ObjetGestion
        {
            ListeObservable<T> pListe = new ListeObservable<T>();

            ListBox V; ListeObservable<T> Liste; List<T> Ls; T L;
            if (Info(sender as MenuItem, out V, out Liste, out Ls, out L))
            {
                U Parent = (U)V.DataContext;
                // On force à faire au minimum un tour de boucle dans le cas ou la liste est vide.

                int Nb = Math.Max(Ls.Count, 1);
                if (UnSeul)
                    Nb = 1;

                for (int i = 0; i < Nb; i++)
                    pListe.Add(Ajouter<T, U>(Parent));
            }

            return pListe;
        }

        private ListeObservable<T> Inserer_List<T>(object sender, RoutedEventArgs e)
            where T : ObjetGestion, new()
        {
            ListeObservable<T> pListe = new ListeObservable<T>();

            ListBox V; ListeObservable<T> Liste; List<T> Ls; T L;
            if (Info(sender as MenuItem, out V, out Liste, out Ls, out L))
            {
                foreach (T iL in Ls)
                {
                    T Obj = Ajouter<T>();
                    pListe.Add(Obj);
                    Liste.Move(Liste.IndexOf(Obj), Liste.IndexOf(L));
                }
                Liste.Numeroter();
            }
            return pListe;
        }

        private ListeObservable<T> Inserer_List<T, U>(object sender, RoutedEventArgs e)
            where T : ObjetGestion, new()
            where U : ObjetGestion
        {
            ListeObservable<T> pListe = new ListeObservable<T>();

            ListBox V; ListeObservable<T> Liste; List<T> Ls; T L;
            if (Info(sender as MenuItem, out V, out Liste, out Ls, out L))
            {
                U Parent = (U)V.DataContext;
                foreach (T iL in Ls)
                {
                    T Obj = Ajouter<T, U>(Parent);
                    pListe.Add(Obj);
                    Liste.Move(Liste.IndexOf(Obj), Liste.IndexOf(L));
                }
                Liste.Numeroter();
            }
            return pListe;
        }

        private void Monter_List<T>(object sender, RoutedEventArgs e)
            where T : ObjetGestion
        {
            ListBox V; ListeObservable<T> Liste; List<T> Ls; T L;
            if (Info(sender as MenuItem, out V, out Liste, out Ls, out L))
            {
                // On test si une ligne n'est pas à la position 0 pour eviter les erreurs
                foreach (T iL in Ls)
                {
                    if (Liste.IndexOf(iL) == 0)
                        return;
                }

                // Si c'est bon, on les déplace toutes
                foreach (T iL in Ls)
                {
                    int De = Liste.IndexOf(iL);

                    Liste.Move(De, De - 1);
                }

                Liste.Numeroter();
            }
        }

        private void Descendre_List<T>(object sender, RoutedEventArgs e)
            where T : ObjetGestion
        {
            ListBox V; ListeObservable<T> Liste; List<T> Ls; T L;
            if (Info(sender as MenuItem, out V, out Liste, out Ls, out L))
            {
                // On test si une ligne n'est pas à la dernière position pour eviter les erreurs
                foreach (T iL in Ls)
                {
                    if (Liste.IndexOf(iL) == (Liste.Count - 1))
                        return;
                }

                // Si c'est bon, on les déplace toutes
                Ls.Reverse();
                foreach (T iL in Ls)
                {
                    int De = Liste.IndexOf(iL);

                    Liste.Move(De, De + 1);
                }

                Liste.Numeroter();
            }
        }

        private void Supprimer_List<T>(object sender, RoutedEventArgs e, Boolean Message = false, Boolean UnItemMini = false)
            where T : ObjetGestion
        {
            ListBox V; ListeObservable<T> Liste; List<T> Ls; T L;
            if (Info(sender as MenuItem, out V, out Liste, out Ls, out L))
            {
                foreach (T iL in Ls)
                {
                    Boolean Supprimer = !Message;

                    if (Liste.Count >= 1)
                    {
                        if (Message && MessageBox.Show(String.Format("Voulez vous vraiement supprimer : {0} {1} ?", DicIntitules.IntituleType(typeof(T).Name), iL.Ref), "Suppression", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                            Supprimer = true;

                        if (Supprimer)
                        {
                            if (UnItemMini == false)
                            {
                                if (iL.Supprimer())
                                    Liste.Remove(iL);
                            }

                        }
                    }
                }
                Liste.Numeroter();
            }
        }

        private void Copier_List<T>(object sender, RoutedEventArgs e)
            where T : ObjetGestion
        {
            ListBox V; ListeObservable<T> Liste; List<T> Ls; T L;
            if (Info(sender as MenuItem, out V, out Liste, out Ls, out L))
            {
                _Copie_Liste = Ls;
            }
        }

        private void Coller_List<T>(object sender, RoutedEventArgs e)
            where T : ObjetGestion
        {
            ListBox V; ListeObservable<T> Liste; List<T> Ls; T L;
            if (Info(sender as MenuItem, out V, out Liste, out Ls, out L) && (_Copie_Liste != null))
            {
                List<T> pListe = new List<T>();

                if (_Copie_Liste != null)
                    pListe = _Copie_Liste as List<T>;

                for (int i = 0; i < Ls.Count; i++)
                {
                    if (i < pListe.Count)
                    {
                        Ls[i].Copier(pListe[i]);
                    }
                }
                Liste.Numeroter();
            }
        }

        private ListeObservable<T> Inserer_Coller_List<T>(object sender, RoutedEventArgs e)
            where T : ObjetGestion, new()
        {
            ListeObservable<T> pListe = new ListeObservable<T>();

            ListBox V; ListeObservable<T> Liste; List<T> Ls; T L;
            if (Info(sender as MenuItem, out V, out Liste, out Ls, out L) && (_Copie_Liste != null))
            {
                List<T> pListeCopie = new List<T>();

                pListeCopie = _Copie_Liste as List<T>;

                foreach (T iL in pListeCopie)
                {
                    T Obj = Ajouter<T>();
                    pListe.Add(Obj);
                    Obj.Copier(iL);

                    // Si la liste contient des lignes, on la déplace
                    if (L != null)
                        Liste.Move(Liste.IndexOf(Obj), Liste.IndexOf(L));
                }
                Liste.Numeroter();
            }
            return pListe;
        }

        private ListeObservable<T> Inserer_Coller_List<T, U>(object sender, RoutedEventArgs e)
            where T : ObjetGestion, new()
            where U : ObjetGestion
        {
            // Liste temporaire renvoyé par la fonction
            ListeObservable<T> pListe = new ListeObservable<T>();

            ListBox V; ListeObservable<T> Liste; List<T> Ls; T L;
            if (Info(sender as MenuItem, out V, out Liste, out Ls, out L) && (_Copie_Liste != null))
            {
                List<T> pListeCopie = new List<T>();

                U Parent = (U)V.DataContext;
                pListeCopie = _Copie_Liste as List<T>;

                foreach (T iL in pListeCopie)
                {
                    // On ajoute une ligne
                    T Obj = Ajouter<T, U>(Parent);
                    pListe.Add(Obj);

                    // On copie la ligne
                    Obj.Copier(iL);

                    // Si la liste contient des lignes, on la déplace
                    if(L != null)
                        Liste.Move(Liste.IndexOf(Obj), Liste.IndexOf(L));
                }

                Liste.Numeroter();
            }
            return pListe;
        }

        #endregion

        #region LISTVIEW

        private void Ajuster_Colonnes(ListView L)
        {
            GridView G = L.View as GridView;
            if (G != null)
            {
                foreach (GridViewColumn C in G.Columns)
                {
                    C.Width = C.ActualWidth;

                    C.Width = double.NaN;
                }
            }
        }

        private void ListView_Loaded(object sender, RoutedEventArgs e)
        {
            Ajuster_Colonnes(FindVisualParent<ListView>(sender as UIElement));
        }

        private void ComboBox_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            Ajuster_Colonnes(FindVisualParent<ListView>(sender as UIElement));
        }

        private void TextBlock_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            Ajuster_Colonnes(FindVisualParent<ListView>(sender as UIElement));
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Ajuster_Colonnes(FindVisualParent<ListView>(sender as UIElement));
        }

        public static T FindVisualParent<T>(UIElement element) where T : UIElement
        {
            UIElement parent = element; while (parent != null)
            {
                T correctlyTyped = parent as T; if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }
                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            } return null;
        }

        private void Control_KeyUp(object sender, KeyEventArgs e)
        {
            Ajuster_Colonnes(FindVisualParent<ListView>(sender as UIElement));
        }

        private void Control_LostFocus(object sender, RoutedEventArgs e)
        {
            Ajuster_Colonnes(FindVisualParent<ListView>(sender as UIElement));
        }

        // Fontion pour eviter le freezz du scroll quand la souris est sur une listview contenue dans une listbox
        private void ListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }

        #endregion

    }
}
