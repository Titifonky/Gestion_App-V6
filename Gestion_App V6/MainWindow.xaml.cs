using LogDebugging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;


namespace Gestion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    ///

    public partial class MainWindow : Window
    {
        public static TabItem DernierOngletActif = null;

        private RechercheTexte<Client> _RechercherClient;
        private RechercheTexte<Devis> _RechercherDevis;
        private RechercheTexte<Facture> _RechercherFactureClient;

        public Societe pSociete;

        public MainWindow()
        {
            this.Closing += new CancelEventHandler(MainWindow_Closing);

            InitializeComponent();

            this.AddHandler(OngletSupprimable.TabItemClosed_Event, new RoutedEventHandler(this.SupprimerOnglet));

            xOnglets.SelectionChanged += this.SelectionOnglet;

            if (!Start()) this.Close();

            WindowParam.AjouterParametre(this);
            WindowParam.RestaurerParametre(this);
        }

        private Boolean Start()
        {
            Log.Entete();

            SelectionnerBase Fenetre = new SelectionnerBase(Bdd.ListeBase());
            Fenetre.ShowDialog();



            if (!Bdd.Initialiser(Fenetre.BaseSelectionnee)) return false;

            xConnexionCourante.Text = Fenetre.BaseSelectionnee + ", connecté à l'adresse : " + Bdd.ConnexionCourante;

            pSociete = Bdd.Liste<Societe>()[0];

            ListeObservable<Ligne_Poste> liste = Bdd.Liste<Ligne_Poste>();

            //Regex rgx = new Regex(@"\.0$");

            //foreach (var L in liste)
            //{
            //    L.Prix_Exp = rgx.Replace(L.Prix_Exp, "");
            //    L.Qte_Exp = rgx.Replace(L.Qte_Exp, "");
            //}

            //Bdd.Enregistrer();

            pSociete.OnModifyUtilisateur += new Societe.OnModifyUtilisateurEventHandler(id => { Properties.Settings.Default.IdUtilisateur = id; Properties.Settings.Default.Save(); });

            ListeObservable<Utilisateur> pListeUtilisateur = pSociete.ListeUtilisateur;

            Utilisateur U = null;

            if (pListeUtilisateur.Count > 0)
            {
                try
                {
                    U = pListeUtilisateur.First(u => { return u.Id == Properties.Settings.Default.IdUtilisateur; });
                }
                catch { U = pListeUtilisateur[0]; }
            }
            else
            {
                U = new Utilisateur(pSociete);
                U.Prefix_Utilisateur = "A";
                Bdd.Ajouter(U);
            }

            pSociete.UtilisateurCourant = U;

            this.DataContext = pSociete;

            TrierListe<Client>(xListeClient);
            TrierListe<Devis>(xListeDevis);
            TrierListe<Facture>(xListeFactureClient);
            TrierListe<Facture>(xListeFactureDevis);

            _RechercherClient = new RechercheTexte<Client>(xListeClient);
            xRechercherClient.DataContext = _RechercherClient;

            _RechercherDevis = new RechercheTexte<Devis>(xListeDevis);
            xRechercherDevis.DataContext = _RechercherDevis;

            _RechercherFactureClient = new RechercheTexte<Facture>(xListeFactureClient);
            xRechercherFactureClient.DataContext = _RechercherFactureClient;

            return true;
        }

        private void TrierListe<T>(ListBox Box)
            where T : ObjetGestion
        {
            List<String> NomCles = new List<String>();
            List<PropertyInfo> pListeTri = Bdd.DicProprietes.ListeTri(typeof(T));

            foreach (PropertyInfo P in pListeTri)
            {
                ListSortDirection Dir = (P.GetCustomAttributes(typeof(Tri)).First() as Tri).DirectionTri;
                Box.Items.SortDescriptions.Add(new SortDescription(P.Name, Dir));
            }
            Box.Items.IsLiveSorting = true;
        }

        public void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            MessageBoxResult R = MessageBoxResult.No;
            if (Bdd.DoitEtreEnregistre)
                R = MessageBox.Show("Voulez vous enregistrer la base ?", "Info", MessageBoxButton.YesNo);

            if (R == MessageBoxResult.Yes)
                Bdd.Enregistrer();

            Bdd.Deconnecter();

            WindowParam.SauverParametre(this);
        }
    }

    public static class WindowParam
    {
        private static String DimEcran
        {
            get
            {
                return System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height + "x" + System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
            }
        }

        public static void AjouterParametre(Window w)
        {
            ConfigModule Cfg = new ConfigModule(DimEcran);

            Cfg.AjouterP<Double>(w.Name + "_Left", w.Left, "");
            Cfg.AjouterP<Double>(w.Name + "_Top", w.Top, "");
            Cfg.AjouterP<Double>(w.Name + "_Width", w.Width, "");
            Cfg.AjouterP<Double>(w.Name + "_Height", w.Height, "");

            Cfg.Sauver();
        }

        public static void RestaurerParametre(Window w)
        {
            ConfigModule Cfg = new ConfigModule(DimEcran);

            w.Left = Cfg.GetP<Double>(w.Name + "_Left").GetValeur<Double>();
            w.Top = Cfg.GetP<Double>(w.Name + "_Top").GetValeur<Double>();
            w.Width = Cfg.GetP<Double>(w.Name + "_Width").GetValeur<Double>();
            w.Height = Cfg.GetP<Double>(w.Name + "_Height").GetValeur<Double>();
        }

        public static void SauverParametre(Window w)
        {
            ConfigModule Cfg = new ConfigModule(DimEcran);

            Cfg.GetP<Double>(w.Name + "_Left").SetValeur<Double>(w.Left);
            Cfg.GetP<Double>(w.Name + "_Top").SetValeur<Double>(w.Top);
            Cfg.GetP<Double>(w.Name + "_Width").SetValeur<Double>(w.Width);
            Cfg.GetP<Double>(w.Name + "_Height").SetValeur<Double>(w.Height);

            Cfg.Sauver();
        }
    }
}