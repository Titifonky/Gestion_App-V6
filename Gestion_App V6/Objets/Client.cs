using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;

namespace Gestion
{
    [ForcerAjout]
    public class Client : ObjetGestion
    {
        public Client() { }

        public Client(Societe S)
        {
            Bdd2.Ajouter(this);

            Societe = S;

            // On rajoute le prefix après pour être sûr qu'il ne sera pas ecrasé par une valeur par defaut
            Prefix_Utilisateur = Societe.PrefixUtilisateurCourant;
        }

        internal String NomDossierClient(Boolean Court = false)
        {
            if (Court)
                return Ref;

            return Ref + " - " + Regex.Replace(Intitule, @"\r\n?|\n", " - ");
        }

        private void CreerStructureDossier()
        {
            DirectoryInfo pDossier = Dossier;
            if (pDossier == null) return;

            // Dossier de base
            String Chemin = Path.Combine(pDossier.FullName, Properties.Settings.Default.NomDossierDevis);
            Directory.CreateDirectory(Chemin);

            Chemin = Path.Combine(pDossier.FullName, Properties.Settings.Default.NomDossierFacture);
            Directory.CreateDirectory(Chemin);

            Chemin = Path.Combine(pDossier.FullName, Properties.Settings.Default.NomFichierInfos);
            if (File.Exists(Chemin))
                File.Delete(Chemin);
        }

        public DirectoryInfo CreerDossier(Boolean Forcer = false)
        {
            try
            {
                String DossierGED = Societe.UtilisateurCourant.Dossier_GED;

                if (Directory.Exists(DossierGED) && (Dossier == null))
                {
                    String Chemin = Path.Combine(DossierGED, NomDossierClient());

                    DirectoryInfo pDossier = Directory.CreateDirectory(Chemin);

                    //CreerStructureDossier();

                    return pDossier;
                }

                if (Forcer && (Dossier != null))
                {
                    String Chemin = Path.Combine(DossierGED, NomDossierClient());
                    if (Dossier.FullName != Chemin)
                        Dossier.MoveTo(Chemin);

                    return Dossier;
                }
            }
            catch { }

            return null;
        }

        public DirectoryInfo Dossier
        {
            get
            {
                String DossierGED = Societe.UtilisateurCourant.Dossier_GED;

                if (Directory.Exists(DossierGED))
                {
                    String Chemin = Path.Combine(DossierGED, NomDossierClient());

                    if (Directory.Exists(Chemin))
                        return new DirectoryInfo(Chemin);

                    String[] ListeDossier = Directory.GetDirectories(DossierGED, NomDossierClient(true) + " *", SearchOption.TopDirectoryOnly);

                    if (ListeDossier.GetLength(0) > 0)
                        return new DirectoryInfo(ListeDossier[0]);    
                }

                return null;
            }
        }

        [Propriete, Max]
        [Tri(No = 2, DirectionTri = ListSortDirection.Descending)]
        public override int No
        {
            get { return base.No; }
            set { base.No = value; }
        }

        private int? _Id_Societe = null;
        private Societe _Societe = null;
        [CleEtrangere]
        public Societe Societe
        {
            get
            {
                if (_Societe == null)
                    _Societe = Bdd2.Parent<Societe, Client>(this);

                return _Societe;
            }
            set
            {
                Set(ref _Societe, value, this);
                if (_Societe.ListeClient != null)
                    _Societe.ListeClient.Add(this);
            }
        }

        private String _Intitule = "";
        [Propriete]
        public String Intitule
        {
            get { return _Intitule; }
            set { Set(ref _Intitule, value, this); }
        }

        private Boolean _Favori = false;
        [Propriete]
        [Tri(No = 1, DirectionTri = ListSortDirection.Descending)]
        public Boolean Favori
        {
            get { return _Favori; }
            set { Set(ref _Favori, value, this); }
        }

        private ListeObservable<Devis> _ListeDevis = null;
        [ListeObjetGestion]
        public ListeObservable<Devis> ListeDevis
        {
            get
            {
                if(_ListeDevis == null)
                    _ListeDevis = Bdd2.Enfants<Devis, Client>(this);

                return _ListeDevis;
            }

            set { SetListe(ref _ListeDevis, value); }
        }

        private ListeObservable<Adresse_Client> _ListeAdresse_Client = null;
        [ListeObjetGestion]
        public ListeObservable<Adresse_Client> ListeAdresse_Client
        {
            get
            {
                if (_ListeAdresse_Client == null)
                    _ListeAdresse_Client = Bdd2.Enfants<Adresse_Client, Client>(this);

                return _ListeAdresse_Client;
            }

            set { SetListe(ref _ListeAdresse_Client, value); }
        }

        /// <summary>
        /// Methode pour simplifier les requetes
        /// </summary>
        private ListeObservable<Facture> _ListeFacture = null;
        [ListeObjetGestion]
        public ListeObservable<Facture> ListeFacture
        {
            get
            {
                if (_ListeFacture == null)
                {
                    _ListeFacture = new ListeObservable<Facture>();

                    foreach (Devis D in ListeDevis)
                    {
                        // Si la liste des factures du devis est nulle, c'est qu'on est en plein chargement du devis
                        // On efface la liste et on sort.
                        // Initialisation la prochaine fois
                        if (D.ListeFacture != null)
                            foreach (Facture F in D.ListeFacture)
                            {
                                if (!_ListeFacture.Contains(F))
                                    _ListeFacture.Add(F);
                            }
                        else
                        {
                            _ListeFacture = null;
                            break;
                        }
                    }
                }

                return _ListeFacture;
            }
            set { SetListe(ref _ListeFacture, value); }
        }

        private ListeAvecTitre<ListeAvecTitre<Object>> _ListeAnalyseDevis = null;
        public ListeAvecTitre<ListeAvecTitre<Object>> ListeAnalyseDevis
        {
            get
            {
                if (_ListeAnalyseDevis == null)
                    Analyser();

                return _ListeAnalyseDevis;
            }
        }

        private ListeAvecTitre<ListeAvecTitre<Object>> _ListeAnalyseFacture = null;
        public ListeAvecTitre<ListeAvecTitre<Object>> ListeAnalyseFacture
        {
            get
            {
                if (_ListeAnalyseFacture == null)
                    Analyser();

                return _ListeAnalyseFacture;
            }
        }

        public void Analyser()
        {
            if (!EstCharge) return;

            if (_ListeAnalyseDevis == null)
                _ListeAnalyseDevis = new ListeAvecTitre<ListeAvecTitre<Object>>("Devis");
            else
                _ListeAnalyseDevis.Clear();

            if (_ListeAnalyseFacture == null)
                _ListeAnalyseFacture = new ListeAvecTitre<ListeAvecTitre<Object>>("Facture");
            else
                _ListeAnalyseFacture.Clear();

            Bdd2.AnalyseClient(ref _ListeAnalyseDevis, ref _ListeAnalyseFacture, Id);
        }

        public override Boolean Supprimer()
        {
            if (!EstCharge) return false;

            SupprimerListe(_ListeDevis);
            SupprimerListe(_ListeAdresse_Client);

            Societe.ListeClient.Remove(this);

            try
            {
                // Suppression des dossiers s'il sont vide
                DirectoryInfo pDossier = Dossier;
                if (pDossier != null)
                {
                    List<DirectoryInfo> ListeDossier = new List<DirectoryInfo>(pDossier.GetDirectories());
                    List<FileInfo> ListeFichier = new List<FileInfo>(pDossier.GetFiles());

                    if ((ListeDossier.Count == 0) && (ListeFichier.Count == 0))
                        Directory.Delete(pDossier.FullName, true);
                }
            }
            catch { }


            Bdd2.Supprimer<Client>(this);

            return true;
        }
    }
}
