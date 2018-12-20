using System;
using System.Collections.ObjectModel;
using System.Data;

namespace Gestion
{
    public class Poste : ObjetGestion
    {
        public Poste() { }

        public Poste(Devis D)
        {
            Devis = D;
            Bdd.Ajouter(this);
            
            // On rajoute le prefix après pour être sûr qu'il ne sera pas ecrasé par une valeur par defaut
            Prefix_Utilisateur = Devis.Client.Societe.PrefixUtilisateurCourant;
            Titre = "Poste " + D.ListePoste.Count;
            No = D.ListePoste.Count;
        }

        [Propriete, Tri(Modifiable=true)]
        public override int No
        {
            get { return base.No; }
            set { base.No = value; }
        }

        private Devis _Devis = null;
        [CleEtrangere]
        public Devis Devis
        {
            get
            {
                if (_Devis == null)
                    _Devis = Bdd.Parent<Devis, Poste>(this);

                return _Devis;
            }
            set
            {
                Set(ref _Devis, value, this);
                if (_Devis.ListePoste != null)
                    _Devis.ListePoste.Add(this);
            }
        }

        private String _Titre = "";
        [Propriete]
        public String Titre
        {
            get { return _Titre; }
            set { Set(ref _Titre, value, this); }
        }

        private String _Description = "";
        [Propriete]
        public String Description
        {
            get { return _Description; }
            set { Set(ref _Description, value, this); }
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

        private Double _Prix_Unitaire = 0;
        [Propriete]
        public Double Prix_Unitaire
        {
            get { return _Prix_Unitaire; }
            set { Set(ref _Prix_Unitaire, value, this); }
        }

        private Double _Prix_Ht = 0;
        [Propriete]
        public Double Prix_Ht
        {
            get { return _Prix_Ht; }
            set { Set(ref _Prix_Ht, value, this); }
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

        private String _Unite = "";
        [Propriete]
        public String Unite
        {
            get { return _Unite; }
            set { Set(ref _Unite, value, this); }
        }

        private Double _Marge_Unitaire = 0;
        [Propriete]
        public Double Marge_Unitaire
        {
            get { return _Marge_Unitaire; }
            set { Set(ref _Marge_Unitaire, value, this); }
        }

        private Double _Marge = 0;
        [Propriete]
        public Double Marge
        {
            get { return _Marge; }
            set { Set(ref _Marge, value, this); }
        }

        private Double _Marge_Pct = 0;
        [Propriete]
        public Double Marge_Pct
        {
            get { return _Marge_Pct; }
            set { Set(ref _Marge_Pct, value, this); }
        }

        private Double _Arrondi = 1;
        [Propriete]
        public Double Arrondi
        {
            get { return _Arrondi; }
            set
            {
                Set(ref _Arrondi, value, this);
                Calculer();
            }
        }

        private Double _Deja_Facture_Ht = 0;
        [Propriete]
        public Double Deja_Facture_Ht
        {
            get { return _Deja_Facture_Ht; }
            set { Set(ref _Deja_Facture_Ht, value, this); }
        }

        private Double _Reste_A_Facture_Ht = 0;
        [Propriete]
        public Double Reste_A_Facture_Ht
        {
            get { return _Reste_A_Facture_Ht; }
            set { Set(ref _Reste_A_Facture_Ht, value, this); }
        }


        private ListeObservable<Ligne_Poste> _ListeLignePoste = null;
        public ListeObservable<Ligne_Poste> ListeLignePoste
        {
            get
            {
                if (_ListeLignePoste == null)
                    _ListeLignePoste = Bdd.Enfants<Ligne_Poste, Poste>(this);

                return _ListeLignePoste;
            }
        }

        private ListeObservable<Ligne_Facture> _ListeLigneFacture = null;
        public ListeObservable<Ligne_Facture> ListeLigneFacture
        {
            get
            {
                if (_ListeLigneFacture == null)
                    _ListeLigneFacture = Bdd.Enfants<Ligne_Facture, Poste>(this);

                return _ListeLigneFacture;
            }
        }

        public void Calculer(Boolean Dependance = true)
        {
            if (!EstCharge) return;

            Double pPrix_Unitaire = 0;
            Double pDebours_Unitaire = 0;

            foreach (Ligne_Poste Ligne in ListeLignePoste)
            {
                if (!Ligne.Statut)
                    continue;

                if (Ligne.Prix_Forfaitaire)
                    Ligne.Calculer(false);

                pPrix_Unitaire += Ligne.Prix_Ht;
                pDebours_Unitaire += Ligne.Debours_Unitaire;
            }

            Prix_Unitaire = Outils.Plafond(pPrix_Unitaire, Arrondi);
            Marge_Unitaire = ArrondiEuro(Prix_Unitaire - pDebours_Unitaire);

            Prix_Ht = Prix_Unitaire * Qte;
            Marge = Marge_Unitaire * Qte;
            //Marge_Pct = ArrondiPct((Marge / Prix_Ht) * 100);
            Marge_Pct = ArrondiPct(((Prix_Ht / (Prix_Ht - Marge)) - 1) * 100);

            CalculerFacture(false);

            if (Dependance && (Devis != null))
                Devis.Calculer();
        }

        public void CalculerFacture(Boolean Dependance = true)
        {
            Double pDeja_Facture_Ht = 0;
            foreach (Ligne_Facture Ligne in ListeLigneFacture)
            {
                if (!Ligne.Statut)
                    continue;

                pDeja_Facture_Ht += Ligne.Ht;
            }

            Deja_Facture_Ht = pDeja_Facture_Ht;
            Reste_A_Facture_Ht = Math.Max(Prix_Ht - Deja_Facture_Ht, 0);

            if (Dependance && (Devis != null))
                Devis.CalculerFacture();
        }

        public override Boolean Supprimer()
        {
            if (!EstCharge) return false;

            SupprimerListe(_ListeLignePoste);
            SupprimerListe(_ListeLigneFacture);

            if (Devis != null)
                Devis.ListePoste.Remove(this);

            Bdd.Supprimer<Poste>(this);

            if (Devis != null)
                Devis.Calculer();

            return true;
        }

        public override void Copier<T>(T ObjetBase)
        {
            Poste PosteBase = ObjetBase as Poste;
            if ((!EstCharge) || (PosteBase == null) || (!PosteBase.EstCharge)) return;

            CopierBase(PosteBase);

            foreach (Ligne_Poste Ligne in PosteBase.ListeLignePoste)
            {
                Ligne_Poste pNewLigne = new Ligne_Poste(this);
                pNewLigne.Copier(Ligne);
            }
        }
    }
}
