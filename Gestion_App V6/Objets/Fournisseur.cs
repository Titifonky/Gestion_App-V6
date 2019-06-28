using System;

namespace Gestion
{
    public class Fournisseur : ObjetGestion
    {
        public Fournisseur() { }

        public Fournisseur(Societe S)
        {
            Bdd2.Ajouter(this);

            Societe = S;
            
            // On rajoute le prefix après pour être sûr qu'il ne sera pas ecrasé par une valeur par defaut
            Prefix_Utilisateur = Societe.PrefixUtilisateurCourant;
        }

        private int? _Id_Societe = null;
        private Societe _Societe = null;
        [CleEtrangere]
        public Societe Societe
        {
            get
            {
                if (_Societe == null)
                    _Societe = Bdd2.Parent<Societe, Fournisseur>(this);

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
        [ListeObjetGestion]
        public ListeObservable<Achat> ListeCommande
        {
            get
            {
                if (_ListeCommande == null)
                    _ListeCommande = Bdd2.Enfants<Achat, Fournisseur>(this);

                return _ListeCommande;
            }
            set { SetListe(ref _ListeCommande, value); }
        }

        public override Boolean Supprimer()
        {
            if (!EstCharge) return false;

            Societe.ListeFournisseur.Remove(this);

            Bdd2.Supprimer(this);

            return true;
        }
    }
}
