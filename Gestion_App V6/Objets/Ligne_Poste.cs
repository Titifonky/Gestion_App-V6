using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace Gestion
{

    public class Ligne_Poste : ObjetGestion
    {
        private Boolean Init = false;

        public Ligne_Poste() { }

        public Ligne_Poste(Poste P)
        {
            Bdd2.Ajouter(this);

            Poste = P;

            // On initialise la famille
            ListeObservable<Famille> F = Poste.Devis.Client.Societe.ListeFamille;
            if ((F != null) && (F.Count > 0))
                Famille = F[0];

            // On rajoute le prefix après pour être sûr qu'il ne sera pas ecrasé par une valeur par defaut
            Prefix_Utilisateur = Poste.Devis.Client.Societe.PrefixUtilisateurCourant;

            No = P.ListeLignePoste.Count + 1;

            Init = true;
            Famille = F[0];

            Init = true;
        }

        [Propriete, Tri(Modifiable=true)]
        public override int No
        {
            get { return base.No; }
            set { base.No = value; }
        }

        private int? _Id_Poste = null;
        private Poste _Poste = null;
        [CleEtrangere]
        public Poste Poste
        {
            get
            {
                if (_Poste == null)
                    _Poste = Bdd2.Parent<Poste, Ligne_Poste>(this);

                return _Poste;
            }
            set
            {
                Set(ref _Poste, value, this);
                if (_Poste.ListeLignePoste != null)
                    _Poste.ListeLignePoste.Add(this);
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

        private int? _Id_Famille = null;
        private Famille _Famille = null;
        // Le champ peut être NULL, donc aucune contrainte de base
        [CleEtrangere(Contrainte=""), ForcerCopie]
        public Famille Famille
        {
            get
            {
                if (_Famille == null)
                    _Famille = Bdd2.Parent<Famille, Ligne_Poste>(this);

                return _Famille;
            }
            set
            {
                Set(ref _Famille, value, this);
                CopierParametresFamille();
                Calculer();
            }
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

        private String _Prix_Exp = "1";
        [Propriete]
        public String Prix_Exp
        {
            get { return _Prix_Exp; }
            set
            {
                // Si la valeur se termine par .0 on le supprime
                Regex rgx = new Regex(@"\.0$");
                value = rgx.Replace(value, "");

                // Pour eviter des mises à jour intempestives
                if (Set(ref _Prix_Exp, value, this))
                {
                    try
                    {
                        // Pour eviter des calcules intempetifs
                        Double Eval = value.Evaluer();
                        if(Eval != Prix)
                            Prix = Eval;
                    }
                    catch { }
                }
            }
        }

        private Double _Prix = 1;
        [Propriete]
        public Double Prix
        {
            get { return _Prix; }
            set
            {
                Set(ref _Prix, ArrondiEuro(value), this);
                Calculer();
            }
        }

        private String _Qte_Exp = "1";
        [Propriete]
        public String Qte_Exp
        {
            get { return _Qte_Exp; }
            set
            {
                // Si la valeur se termine par .0 on le supprime
                Regex rgx = new Regex(@"\.0$");
                value = rgx.Replace(value, "");

                // Pour eviter des mises à jour intempestives
                if (Set(ref _Qte_Exp, value, this))
                {
                    try
                    {
                        // Pour eviter des calcules intempetifs
                        Double Eval = value.Evaluer();
                        if (Eval != Qte)
                            Qte = Eval;
                    }
                    catch { }
                }
            }
        }

        private Double _Qte = 1;
        [Propriete]
        public Double Qte
        {
            get { return _Qte; }
            set
            {
                Set(ref _Qte, ArrondiEuro(value), this);
                Calculer();
            }
        }

        private Double _Coef = 1;
        [Propriete]
        public Double Coef
        {
            get { return _Coef; }
            set
            {
                Set(ref _Coef, value, this);
                Calculer();
            }
        }

        private Double _Debours = 0;
        [Propriete]
        public Double Debours
        {
            get { return _Debours; }
            set { Set(ref _Debours, value, this); }
        }

        private Double _Debours_Unitaire = 0;
        [Propriete]
        public Double Debours_Unitaire
        {
            get { return _Debours_Unitaire; }
            set { Set(ref _Debours_Unitaire, value, this); }
        }

        private Double _Prix_Ht = 0;
        [Propriete]
        public Double Prix_Ht
        {
            get { return _Prix_Ht; }
            set { Set(ref _Prix_Ht, value, this); }
        }

        private Double _Marge = 0;
        [Propriete]
        public Double Marge
        {
            get { return _Marge; }
            set { Set(ref _Marge, value, this); }
        }

        private Boolean _Prix_Forfaitaire = false;
        [Propriete]
        public Boolean Prix_Forfaitaire
        {
            get { return _Prix_Forfaitaire; }
            set
            {
                Set(ref _Prix_Forfaitaire, value, this);
                Calculer();
            }
        }

        private void CopierParametresFamille()
        {
            if (!EstCharge) return;

            // Si l'objet n'est pas initialisé, on ne fait rien
            if (!Init) return;

            Description = Famille.Description;
            Unite = Famille.Unite;
            Prix_Exp = Famille.Prix_Exp;
            Qte_Exp = Famille.Qte_Exp;
            Coef = Famille.Coef;
            Prix_Forfaitaire = Famille.Prix_Forfaitaire;

            Init = false;
        }

        public void Calculer(Boolean Dependance = true)
        {
            if (!EstCharge) return;

            if (_Prix_Forfaitaire)
            {
                Debours = ArrondiEuro(Prix * Qte);
                Debours_Unitaire = ArrondiEuro(Debours / Poste.Qte);
            }
            else
            {
                Debours = ArrondiEuro(Prix * Qte) * Poste.Qte;
                Debours_Unitaire = ArrondiEuro(Prix * Qte);
            }

            Prix_Ht = ArrondiEuro(Debours_Unitaire * Coef);

            if (Famille == null)
                Famille = Poste.Devis.Client.Societe.ListeFamille[0];

            Debours_Unitaire = (Famille.Code != CodeFamille_e.cMarge).BoolToInt() * Debours_Unitaire;

            Marge = ArrondiEuro(Prix_Ht - Debours_Unitaire);
            
            if (Dependance)
                Poste.Calculer();
        }

        public override Boolean Supprimer()
        {
            if (!EstCharge) return false;

            if(Poste != null)
                Poste.ListeLignePoste.Remove(this);

            Bdd2.Supprimer<Ligne_Poste>(this);

            if (Poste != null)
                Poste.Calculer();

            return true;
        }
    }
}
