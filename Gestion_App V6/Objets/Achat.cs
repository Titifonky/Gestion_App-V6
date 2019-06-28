using System;
using System.Collections.Generic;
using System.Data;

namespace Gestion
{
    public enum StatutAchat_e
    {
        cEnCours = 1,
        cRegle = 4,
        cAnnule = 5
    }

    public class Achat : ObjetGestion
    {
        public Achat() { }

        public Achat(Devis D)
        {
            Devis = D;

            Bdd1.Ajouter(this);
            
            // On initialise le fournisseur
            ListeObservable<Fournisseur> F = Devis.Client.Societe.ListeFournisseur;
            if((F != null) && (F.Count > 0))
                Fournisseur = F[0];

            
            // On rajoute le prefix après pour être sûr qu'il ne sera pas ecrasé par une valeur par defaut
            Prefix_Utilisateur = Devis.Client.Societe.PrefixUtilisateurCourant;
        }

        private String _No_Achat = "";
        [Propriete]
        public String No_Achat
        {
            get { return _No_Achat; }
            set { Set(ref _No_Achat, value, this); }
        }

        private Fournisseur _Fournisseur = null;
        [CleEtrangere(Contrainte=""), ForcerCopie]
        public Fournisseur Fournisseur
        {
            get
            {
                if (_Fournisseur == null)
                    _Fournisseur = Bdd1.Parent<Fournisseur, Achat>(this);

                return _Fournisseur;
            }
            set { Set(ref _Fournisseur, value, this); }
        }

        private Devis _Devis = null;
        [CleEtrangere]
        public Devis Devis
        {
            get
            {
                if (_Devis == null)
                    _Devis = Bdd1.Parent<Devis, Achat>(this);

                return _Devis;
            }
            set
            {
                Set(ref _Devis, value, this);
                if (_Devis.ListeAchat != null)
                    _Devis.ListeAchat.Add(this);
            }
        }

        private String _Description = "";
        [Propriete]
        public String Description
        {
            get { return _Description; }
            set { Set(ref _Description, value, this); }
        }

        private StatutAchat_e _Statut = StatutAchat_e.cEnCours;
        [Propriete]
        public StatutAchat_e Statut
        {
            get { return _Statut; }
            set { Set(ref _Statut, value, this); }
        }

        private Double _Prix = 0;
        [Propriete]
        public Double Prix
        {
            get { return _Prix; }
            set
            {
                Set(ref _Prix, value, this);
                Calculer();
            }
        }

        public void Calculer()
        {
            if (!EstCharge) return;

            Devis.CalculerAchat();
        }

        public override Boolean Supprimer()
        {
            if (!EstCharge) return false;

            Devis.ListeAchat.Remove(this);
            Devis.CalculerAchat();

            Bdd1.Supprimer<Achat>(this);

            return true;
        }
    }
}
