using System;
using System.ComponentModel;

namespace Gestion
{
    public enum CodeFamille_e
    {
        [Description("Main d'oeuvre")]
        [Unite("h")]
        cMainOeuvre = 1,
        [Description("Achat")]
        [Unite("u")]
        cAchat = 2,
        cMarge = 3
    }

    public class Famille : ObjetGestion
    {
        public Famille() { }

        public Famille(Societe S)
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
                    _Societe = Bdd1.Parent<Societe, Famille>(this);

                return _Societe;
            }
            set
            {
                Set(ref _Societe, value, this);
                if (_Societe.ListeFamille != null)
                    _Societe.ListeFamille.Add(this);
            }
        }

        [Propriete, Max, Tri(Modifiable=true)]
        public override int No
        {
            get { return base.No; }
            set { base.No = value; }
        }

        private CodeFamille_e _Code = CodeFamille_e.cMainOeuvre;
        [Propriete]
        public CodeFamille_e Code
        {
            get { return _Code; }
            set { Set(ref _Code, value, this); }
        }

        private String _Intitule = "";
        [Propriete]
        public String Intitule
        {
            get { return _Intitule; }
            set { Set(ref _Intitule, value, this); }
        }

        private String _Description = "";
        [Propriete]
        public String Description
        {
            get { return _Description; }
            set { Set(ref _Description, value, this); }
        }

        private String _Unite = "";
        [Propriete]
        public String Unite
        {
            get { return _Unite; }
            set { Set(ref _Unite, value, this); }
        }

        private Double _Prix = 0;
        [Propriete]
        public Double Prix
        {
            get { return _Prix; }
            set { Set(ref _Prix, value, this); }
        }

        private String _Prix_Exp = "";
        [Propriete]
        public String Prix_Exp
        {
            get { return _Prix_Exp; }
            set
            {
                Set(ref _Prix_Exp, value, this);
                try
                {
                    Prix = value.Evaluer();
                }
                catch { }
            }
        }

        private Double _Qte = 1;
        [Propriete]
        public Double Qte
        {
            get { return _Qte; }
            set { Set(ref _Qte, value, this); }
        }

        private String _Qte_Exp = "";
        [Propriete]
        public String Qte_Exp
        {
            get { return _Qte_Exp; }
            set
            {
                Set(ref _Qte_Exp, value, this);
                try
                {
                    Qte = value.Evaluer();
                }
                catch { }
            }
        }

        private Double _Coef = 1;
        [Propriete]
        public Double Coef
        {
            get { return _Coef; }
            set { Set(ref _Coef, value, this); }
        }

        private Boolean _Prix_Forfaitaire = false;
        [Propriete]
        public Boolean Prix_Forfaitaire
        {
            get { return _Prix_Forfaitaire; }
            set { Set(ref _Prix_Forfaitaire, value, this); }
        }

        private Boolean _Supprimable = true;
        [Propriete]
        public Boolean Supprimable
        {
            get { return _Supprimable; }
            set { Set(ref _Supprimable, value, this); }
        }

        public override Boolean Supprimer()
        {
            if (!EstCharge) return false;

            if (Supprimable)
            {
                Societe.ListeFamille.Remove(this);

                Bdd1.Supprimer<Famille>(this);
                return true;
            }

            return false;
        }
    }
}
