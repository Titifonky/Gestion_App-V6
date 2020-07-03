using System;
using System.ComponentModel;
using System.Linq;

namespace Gestion
{
    public enum StatutFacture_e
    {
        [Description("En cours")]
        cEnCours = 1,
        [Description("Envoyé")]
        cEnvoye = 2,
        [Description("Reglé")]
        cRegle = 3,
        [Description("Annulé")]
        cAnnule = 4
    }

    [ForcerAjout]
    public partial class Facture : ObjetGestion
    {
        public Facture() { }

        public Facture(Devis D)
        {
            Bdd2.Ajouter(this);

            Devis = D;

            int pNo = No;
            
            int pIndice = 0;

            // On recherche le numero et l'indice de la facture
            if (D.ListeFacture.Count > 0)
            {
                pNo = D.ListeFacture[0].No;
                pIndice = Math.Max(D.ListeFacture.Max(x => x.Indice) + 1, D.ListeFacture.Count);
            }

            // Si on rajoute une facture, c'est que le devis est au moins validé
            if ((int)Devis.Statut < (int)StatutDevis_e.cValide)
                Devis.Statut = StatutDevis_e.cValide;
            
            // On rajoute le prefix après pour être sûr qu'il ne sera pas ecrasé par une valeur par defaut
            Prefix_Utilisateur = Devis.Client.Societe.PrefixUtilisateurCourant;
            
            // On met à jour le no et l'indice après l'ajout de l'objet dans la base

            No = pNo; Indice = pIndice;

            MajLigne_Facture();
        }

        public void MajLigne_Facture()
        {
            SupprimerListe(_ListeLigneFacture);

            // On rajoute les lignes correspondant à chaque poste.
            foreach (Poste Poste in Devis.ListePoste)
            {
                if (Poste.Statut)
                    new Ligne_Facture(this, Poste);
            }
        }

        protected override void MajRef(String reference = null)
        {
            base.MajRef(Ref = Prefix_Utilisateur + "F" + No + "-" + Indice);
        }

        private int _Indice = 0;
        [Propriete]
        [Tri(No = 3, DirectionTri = ListSortDirection.Descending)]
        public int Indice
        {
            get { return _Indice; }
            set
            {
                Set(ref _Indice, value, this);
                MajRef();
            }
        }

        private int? _Id_Devis = null;
        private Devis _Devis = null;
        [CleEtrangere]
        public Devis Devis
        {
            get
            {
                if (_Devis == null)
                    _Devis = Bdd2.Parent<Devis, Facture>(this);

                return _Devis;
            }
            set
            {
                Set(ref _Devis, value, this);

                if (_Devis.ListeFacture != null)
                    _Devis.ListeFacture.Add(this);

                if (_Devis.Client.ListeFacture != null)
                    _Devis.Client.ListeFacture.Add(this);
            }
        }

        private StatutFacture_e _Statut = StatutFacture_e.cEnCours;
        [Propriete]
        [Tri(No = 2, DirectionTri = ListSortDirection.Ascending)]
        public StatutFacture_e Statut
        {
            get { return _Statut; }
            set { Set(ref _Statut, value, this); }
        }

        private Boolean _Compta = false;
        [Propriete]
        public Boolean Compta
        {
            get { return _Compta; }
            set { Set(ref _Compta, value, this); }
        }

        private DateTime _Date = DateTime.Now;
        [Propriete]
        public DateTime Date
        {
            get { return _Date; }
            set { Set(ref _Date, value, this); }
        }

        private Boolean _Imprimer_Commentaires = false;
        [Propriete]
        public Boolean Imprimer_Commentaires
        {
            get { return _Imprimer_Commentaires; }
            set { Set(ref _Imprimer_Commentaires, value, this); }
        }

        private String _Commentaires = "";
        [Propriete]
        public String Commentaires
        {
            get { return _Commentaires; }
            set { Set(ref _Commentaires, value, this); }
        }

        private String _Conditions = "Paiement à 45 jours date de facturation";
        [Propriete]
        public String Conditions
        {
            get { return _Conditions; }
            set { Set(ref _Conditions, value, this); }
        }

        private Double _Prix_Ht = 0;
        [Propriete]
        public Double Prix_Ht
        {
            get { return _Prix_Ht; }
            set { Set(ref _Prix_Ht, value, this); }
        }

        private Double _Tva = 0;
        [Propriete]
        public Double Tva
        {
            get { return _Tva; }
            set { Set(ref _Tva, value, this); }
        }

        private Double _Prix_Ttc = 0;
        [Propriete]
        public Double Prix_Ttc
        {
            get { return _Prix_Ttc; }
            set { Set(ref _Prix_Ttc, value, this); }
        }

        private Boolean _Favori = false;
        [Propriete]
        [Tri(No = 1, DirectionTri = ListSortDirection.Descending)]
        public Boolean Favori
        {
            get { return _Favori; }
            set { Set(ref _Favori, value, this); }
        }

        private ListeObservable<Ligne_Facture> _ListeLigneFacture = null;
        [ListeObjetGestion]
        public ListeObservable<Ligne_Facture> ListeLigneFacture
        {
            get
            {
                if (_ListeLigneFacture == null)
                    _ListeLigneFacture = Bdd2.Enfants<Ligne_Facture, Facture>(this);

                return _ListeLigneFacture;
            }
            set { SetListe(ref _ListeLigneFacture, value); }
        }

        public void Calculer(Boolean Dependance = true)
        {
            if (!EstCharge) return;

            Double pPrix_Ht = 0;

            foreach (Ligne_Facture Ligne in ListeLigneFacture)
            {
                if (!Ligne.Statut)
                    continue;

                pPrix_Ht += Ligne.Ht;
            }

            Prix_Ht = pPrix_Ht;

            Prix_Ttc = Math.Round(Prix_Ht * ((Devis.Tva_Pct + 100) / 100), DEFAULT_ARRONDI_EURO, MidpointRounding.ToEven);

            Tva = Math.Round(Prix_Ttc - Prix_Ht, DEFAULT_ARRONDI_EURO);

            if (Dependance)
                Devis.CalculerFacture();
        }

        public override Boolean Supprimer()
        {
            if (!EstCharge) return false;

            SupprimerListe(_ListeLigneFacture);

            if (Devis != null)
                Devis.ListeFacture.Remove(this);

            if (Devis.Client != null)
                Devis.Client.ListeFacture.Remove(this);

            Bdd2.Supprimer(this);

            if (Devis != null)
                Devis.CalculerFacture();

            return true;
        }
    }
}
