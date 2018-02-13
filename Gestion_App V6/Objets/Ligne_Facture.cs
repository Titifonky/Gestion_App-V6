using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Gestion
{
    public enum CalculLigne_Facture_e
    {
        [Description("Quantite")]
        cQuantite = 1,
        [Description("Pct unitaire")]
        [Unite("%")]
        cPourcentage = 2,
        [Description("Pct total")]
        [Unite("%")]
        cPourcentageTotal = 3
    }

    public class Ligne_Facture : ObjetGestion
    {
        public Ligne_Facture() { }

        public Ligne_Facture(Facture F, Poste P)
        {
            Facture = F;
            Poste = P;

            Bdd.Ajouter(this);

            No = P.No;
            
            // On rajoute le prefix après pour être sûr qu'il ne sera pas ecrasé par une valeur par defaut
            Prefix_Utilisateur = Facture.Devis.Client.Societe.PrefixUtilisateurCourant;

            // Pre-remplissage des champs

            ListeObservable<Ligne_Facture> ListeLigneFacture = P.ListeLigneFacture;

            if (ListeLigneFacture.Count > 0)
            {
                CalculLigne_Facture = ListeLigneFacture[0].CalculLigne_Facture;

                Double pQuantite = P.Qte;
                Double pHt_Unitaire = P.Prix_Unitaire;


                if (CalculLigne_Facture == CalculLigne_Facture_e.cPourcentage)
                {
                    pQuantite = 100;
                    pHt_Unitaire = P.Prix_Ht;
                }

                foreach (Ligne_Facture Lf in ListeLigneFacture)
                {
                    if (Lf == this) continue;

                    pQuantite -= Lf.Qte;
                    pHt_Unitaire -= Lf.Ht_Unitaire;
                }

                if (pQuantite <= 0)
                    pQuantite = P.Qte;

                if (pHt_Unitaire <= 0)
                    pHt_Unitaire = P.Prix_Unitaire;

                Qte = pQuantite;

                if (CalculLigne_Facture == CalculLigne_Facture_e.cPourcentage)
                    Ht_Unitaire = P.Prix_Ht;
                else
                    Ht_Unitaire = pHt_Unitaire;
            }
            else
            {
                Ht_Unitaire = P.Prix_Unitaire;
                Qte = P.Qte;
            }

            Calculer();
        }

        private Facture _Facture = null;
        [CleEtrangere]
        public Facture Facture
        {
            get
            {
                if (_Facture == null)
                    _Facture = Bdd.Parent<Facture, Ligne_Facture>(this);

                return _Facture;
            }
            set
            {
                Set(ref _Facture, value, this);
                if (_Facture.ListeLigneFacture != null)
                    _Facture.ListeLigneFacture.Add(this);
            }
        }

        private Poste _Poste = null;
        [CleEtrangere, ForcerCopie]
        public Poste Poste
        {
            get
            {
                if (_Poste == null)
                    _Poste = Bdd.Parent<Poste, Ligne_Facture>(this);

                return _Poste;
            }
            set
            {
                Set(ref _Poste, value, this);
                if (_Poste.ListeLigneFacture != null)
                    _Poste.ListeLigneFacture.Add(this);
            }
        }

        private Boolean _Statut = true;
        [Propriete]
        public Boolean Statut
        {
            get { return _Statut; }
            set
            {
                Set(ref _Statut, value, this);
                Calculer();
            }
        }

        private Boolean _Imprimer_Description = true;
        [Propriete]
        public Boolean Imprimer_Description
        {
            get { return _Imprimer_Description; }
            set { Set(ref _Imprimer_Description, value, this); }
        }

        private String _Description = "";
        [Propriete]
        public String Description
        {
            get { return _Description; }
            set { Set(ref _Description, value, this); }
        }

        private Double _Ht_Unitaire = 0;
        [Propriete]
        public Double Ht_Unitaire
        {
            get { return _Ht_Unitaire; }
            set
            {
                Set(ref _Ht_Unitaire, value, this);
                Calculer();
            }
        }

        private Double _Qte = 0;
        [Propriete]
        public Double Qte
        {
            get { return _Qte; }
            set
            {
                Set(ref _Qte, value, this);
                Calculer();
            }
        }

        private CalculLigne_Facture_e _CalculLigne_Facture = CalculLigne_Facture_e.cQuantite;
        [Propriete]
        public CalculLigne_Facture_e CalculLigne_Facture
        {
            get { return _CalculLigne_Facture; }
            set
            {
                Set(ref _CalculLigne_Facture, value, this);

                Unite = SelectUnite();

                switch (value)
                {
                    case CalculLigne_Facture_e.cQuantite:
                        Ht_Unitaire = Poste.Prix_Unitaire;
                        break;
                    case CalculLigne_Facture_e.cPourcentage:
                        Ht_Unitaire = Poste.Prix_Unitaire;
                        break;
                    case CalculLigne_Facture_e.cPourcentageTotal:
                        Ht_Unitaire = Poste.Prix_Ht;
                        break;
                    default:
                        break;
                }
                Calculer();
            }
        }

        private String SelectUnite()
        {
            String U = _CalculLigne_Facture.GetEnumUnite();
            if (String.IsNullOrWhiteSpace(U) && (Poste != null))
                U = Poste.Unite;

            if (String.IsNullOrEmpty(U))
                U = " ";

            return U;
        }

        private String _Unite = null;
        public String Unite
        {
            get
            {
                if (_Unite == null)
                    _Unite = SelectUnite();

                return _Unite;
            }
            set { Set(ref _Unite, value, this); }
        }

        private Double _Ht = 0;
        [Propriete]
        public Double Ht
        {
            get { return _Ht; }
            set { Set(ref _Ht, value, this); }
        }

        public void Calculer(Boolean Dependance = true)
        {
            if (!EstCharge) return;

            Ht = Ht_Unitaire * Qte;

            switch (CalculLigne_Facture)
            {
                case CalculLigne_Facture_e.cQuantite:
                    break;
                case CalculLigne_Facture_e.cPourcentage:
                    Ht = Ht * 0.01;
                    break;
                case CalculLigne_Facture_e.cPourcentageTotal:
                    Ht = Ht * 0.01;
                    break;
                default:
                    break;
            }

            if (Dependance)
            {
                if(Poste != null)
                    Poste.CalculerFacture();

                Facture.Calculer();
            }
        }

        public override Boolean Supprimer()
        {
            if (!EstCharge) return false;

            if (Facture != null)
                Facture.ListeLigneFacture.Remove(this);

            if (Poste != null)
            {
                Poste.ListeLigneFacture.Remove(this);
                Poste.CalculerFacture();
            }

            Bdd.Supprimer<Ligne_Facture>(this);
            return true;
        }
    }
}
