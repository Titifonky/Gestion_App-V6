using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;

namespace Gestion
{
    public class Adresse_Client : ObjetGestion
    {
        public Adresse_Client() { }

        public Adresse_Client(Client C)
        {
            Client = C;
            Bdd1.Ajouter(this);

            // On rajoute le prefix après pour être sûr qu'il ne sera pas ecrasé par une valeur par defaut
            Prefix_Utilisateur = Client.Societe.PrefixUtilisateurCourant;
        }

        [Propriete, Max]
        [Tri]
        public override int No
        {
            get { return base.No; }
            set { base.No = value; }
        }

        private Client _Client = null;
        [CleEtrangere]
        public Client Client
        {
            get
            {
                if (_Client == null)
                    _Client = Bdd1.Parent<Client, Adresse_Client>(this);

                return _Client;
            }
            set
            {
                Set(ref _Client, value, this);
                if (_Client.ListeAdresse_Client != null)
                    _Client.ListeAdresse_Client.Add(this);
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

        public override Boolean Supprimer()
        {
            if (!EstCharge) return false;

            if (Client != null)
                Client.ListeAdresse_Client.Remove(this);

            Bdd1.Supprimer<Adresse_Client>(this);

            return true;
        }
    }
}
