using System;

namespace Gestion
{
    public class Fournisseur : ObjetGestion
    {
        public Fournisseur() { }

        public Fournisseur(Societe S)
        {
            Societe = S;
            Bdd1.Ajouter(this);
            
            // On rajoute le prefix après pour être sûr qu'il ne sera pas ecrasé par une valeur par defaut
            Prefix_Utilisateur = Societe.PrefixUtilisateurCourant;
        }

        private Societe _Societe = null;
        [CleEtrangere]
        public Societe Societe
        {
            get
            {
                if (_Societe == null)
                    _Societe = Bdd1.Parent<Societe, Fournisseur>(this);

                return _Societe;
            }
            set
            {
                Set(ref _Societe, value, this);
                if (_Societe.ListeFournisseur != null)
                    _Societe.ListeFournisseur.Add(this);
            }
        }

        private String _Intitule = "";
        [Propriete]
        public String Intitule
        {
            get { return _Intitule; }
            set { Set(ref _Intitule, value, this); }
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

        private ListeObservable<Achat> _ListeCommande = null;
        public ListeObservable<Achat> ListeCommande
        {
            get
            {
                if (_ListeCommande == null)
                    _ListeCommande = Bdd1.Enfants<Achat, Fournisseur>(this);

                return _ListeCommande;
            }
        }

        public override Boolean Supprimer()
        {
            if (!EstCharge) return false;

            Societe.ListeFournisseur.Remove(this);

            Bdd1.Supprimer(this);

            return true;
        }
    }
}
