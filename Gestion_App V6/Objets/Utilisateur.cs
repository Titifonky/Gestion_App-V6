using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;

namespace Gestion
{
    [ForcerAjout]
    public class Utilisateur : ObjetGestion
    {
        public Utilisateur() { }

        public Utilisateur(Societe S)
        {
            Bdd2.Ajouter(this);

            Societe = S;

            // On rajoute le prefix après pour être sûr qu'il ne sera pas ecrasé par une valeur par defaut
            Prefix_Utilisateur = Societe.Prefix_Utilisateur;
        }

        private String _Intitule = "";
        [Propriete]
        public String Intitule
        {
            get { return _Intitule; }
            set { Set(ref _Intitule, value, this); }
        }

        private String ModuleParam = "UtilisateurGED";

        private String _Dossier_GED = "";
        
        public String Dossier_GED
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_Dossier_GED))
                {
                    ConfigModule Cfg = new ConfigModule(ModuleParam);
                    Parametre GED = Cfg.GetP<String>(Ref);
                    if (GED != null)
                        _Dossier_GED = GED.GetValeur<String>();
                }

                return _Dossier_GED;
            }
            set
            {
                ConfigModule Cfg = new ConfigModule(ModuleParam);
                Parametre GED = Cfg.AjouterP<String>(Ref, "Dossier GED");
                GED.SetValeur(value);
                Cfg.Sauver();
                _Dossier_GED = value;
            }
        }

        private int? _Id_Societe = null;
        private Societe _Societe = null;
        [CleEtrangere]
        public Societe Societe
        {
            get { return _Societe; }
            set
            {
                Set(ref _Societe, value, this);
                if (_Societe.ListeUtilisateur != null)
                    _Societe.ListeUtilisateur.Add(this);
            }
        }

        private Boolean _EcraserIndicePDF = false;
        [Propriete]
        public Boolean EcraserIndicePDF
        {
            get { return _EcraserIndicePDF; }
            set { Set(ref _EcraserIndicePDF, value, this); }
        }

        private Boolean _CreerPDF = true;
        [Propriete]
        public Boolean CreerPDF
        {
            get { return _CreerPDF; }
            set { Set(ref _CreerPDF, value, this); }
        }

        private Boolean _MajVignette = true;
        [Propriete]
        public Boolean MajVignette
        {
            get { return _MajVignette; }
            set { Set(ref _MajVignette, value, this); }
        }

        public override Boolean Supprimer()
        {
            if (!EstCharge) return false;

            Societe.ListeUtilisateur.Remove(this);

            Bdd2.Supprimer<Utilisateur>(this);

            return true;
        }
    }
}
