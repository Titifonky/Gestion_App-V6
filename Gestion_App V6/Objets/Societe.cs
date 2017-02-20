using System;
using System.Collections.ObjectModel;
using System.Data;

namespace Gestion
{
    [ForcerAjout]
    public class Societe : ObjetGestion
    {
        public Societe() { Bdd.Ajouter(this); }

        private String _Statut = "";
        [Propriete]
        public String Statut
        {
            get { return _Statut; }
            set { Set(ref _Statut, value, this); }
        }

        private String _Denomination = "";
        [Propriete]
        public String Denomination
        {
            get { return _Denomination; }
            set { Set(ref _Denomination, value, this); }
        }

        private String _Tel_Mobile = "";
        [Propriete]
        public String Tel_Mobile
        {
            get { return _Tel_Mobile; }
            set { Set(ref _Tel_Mobile, value, this); }
        }

        private String _Tel_Fixe = "";
        [Propriete]
        public String Tel_Fixe
        {
            get { return _Tel_Fixe; }
            set { Set(ref _Tel_Fixe, value, this); }
        }

        private String _Email = "";
        [Propriete]
        public String Email
        {
            get { return _Email; }
            set { Set(ref _Email, value, this); }
        }

        private String _Adresse = "";
        [Propriete]
        public String Adresse
        {
            get { return _Adresse; }
            set { Set(ref _Adresse, value, this); }
        }

        private String _Cp = "";
        [Propriete]
        public String Cp
        {
            get { return _Cp; }
            set { Set(ref _Cp, value, this); }
        }

        private String _Ville = "";
        [Propriete]
        public String Ville
        {
            get { return _Ville; }
            set { Set(ref _Ville, value, this); }
        }

        private String _Pied = "";
        [Propriete]
        public String Pied
        {
            get { return _Pied; }
            set { Set(ref _Pied, value, this); }
        }

        public delegate void OnModifyUtilisateurEventHandler(int id);

        public event OnModifyUtilisateurEventHandler OnModifyUtilisateur;

        private Utilisateur _UtilisateurCourant = null;
        public Utilisateur UtilisateurCourant
        {
            get { return _UtilisateurCourant; }
            set
            {
                Set(ref _UtilisateurCourant, value);
                OnModifyUtilisateur(_UtilisateurCourant.Id);
            }
        }

        public String PrefixUtilisateurCourant
        {
            get { return _UtilisateurCourant.Prefix_Utilisateur; }
        }

        private ListeObservable<Client> _ListeClient = null;
        public ListeObservable<Client> ListeClient
        {
            get
            {
                if (_ListeClient == null)
                    _ListeClient = Bdd.Enfants<Client, Societe>(this);

                return _ListeClient;
            }
        }

        private ListeObservable<Fournisseur> _ListeFournisseur = null;
        public ListeObservable<Fournisseur> ListeFournisseur
        {
            get
            {
                if (_ListeFournisseur == null)
                    _ListeFournisseur = Bdd.Enfants<Fournisseur, Societe>(this);

                return _ListeFournisseur;
            }
        }

        private ListeObservable<Famille> _ListeFamille = null;
        public ListeObservable<Famille> ListeFamille
        {
            get
            {
                if (_ListeFamille == null)
                    _ListeFamille = Bdd.Enfants<Famille, Societe>(this);

                return _ListeFamille;
            }
        }

        private ListeObservable<Utilisateur> _ListeUtilisateur = null;
        public ListeObservable<Utilisateur> ListeUtilisateur
        {
            get
            {
                if (_ListeUtilisateur == null)
                    _ListeUtilisateur = Bdd.Enfants<Utilisateur, Societe>(this);

                return _ListeUtilisateur;
            }
        }

        private ListeAvecTitre<ListeAvecTitre<Object>> _ListeAnalyseDevis = null;
        public ListeAvecTitre<ListeAvecTitre<Object>> ListeAnalyseDevis
        {
            get
            {
                Analyser();

                return _ListeAnalyseDevis;
            }
        }

        private ListeAvecTitre<ListeAvecTitre<Object>> _ListeAnalyseFacture = null;
        public ListeAvecTitre<ListeAvecTitre<Object>> ListeAnalyseFacture
        {
            get
            {
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

            Bdd.AnalyseSociete(ref _ListeAnalyseDevis, ref _ListeAnalyseFacture, Id);
        }
    }
}
