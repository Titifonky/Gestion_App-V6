using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;

namespace Gestion
{
    public class Heure : ObjetGestion
    {
        public Heure() { }

        public Heure(Devis D)
        {
            Devis = D;
            Bdd1.Ajouter(this);
            
            // On rajoute le prefix après pour être sûr qu'il ne sera pas ecrasé par une valeur par defaut
            Prefix_Utilisateur = Devis.Client.Societe.PrefixUtilisateurCourant;
        }

        private Devis _Devis = null;
        [CleEtrangere]
        public Devis Devis
        {
            get
            {
                if (_Devis == null)
                    _Devis = Bdd1.Parent<Devis, Heure>(this);

                return _Devis;
            }
            set
            {
                Set(ref _Devis, value, this);
                if (_Devis.ListeHeure != null)
                    _Devis.ListeHeure.Add(this);
            }
        }

        private String _Description = "";
        [Propriete]
        public String Description
        {
            get { return _Description; }
            set { Set(ref _Description, value, this); }
        }

        private Double _Prix = 0;
        [Propriete]
        public Double Prix
        {
            get { return _Prix; }
            set { Set(ref _Prix, value, this); }
        }

        private Double _Qte = 0;
        [Propriete]
        public Double Qte
        {
            get { return _Qte; }
            set { Set(ref _Qte, value, this); }
        }

        private Double _Prix_Ht = 0;
        [Propriete]
        public Double Prix_Ht
        {
            get { return _Prix_Ht; }
            set { Set(ref _Prix_Ht, value, this); }
        }

        public override Boolean Supprimer()
        {
            if (!EstCharge) return false;

            Devis.ListeHeure.Remove(this);

            Bdd1.Supprimer<Heure>(this);

            return true;
        }
    }
}
