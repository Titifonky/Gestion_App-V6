using LogDebugging;
using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace Gestion
{
    // Dictionnaire des intitulé de champ et des valeurs par défaut.
    // En lien avec le fichier MappingDesIntitules.xml
    public static class DicIntitules
    {
        private static String FichierMapping = "MappingDesIntitules.xml";

        private static Dictionary<String, Dictionary<String, Data>> _DicType = null;

        private static Dictionary<String, Data> _DicIntituleType = null;

        private static Dictionary<String, Dictionary<int, String>> _DicEnum = null;

        static DicIntitules()
        {

            String pChemin = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @FichierMapping);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(pChemin);

            _DicType = new Dictionary<String, Dictionary<String, Data>>();
            _DicIntituleType = new Dictionary<String, Data>();

            // Fonction pour renvoyer dans tout les cas une chaine
            Func<XmlNode, String, String, String> Valeur = delegate(XmlNode Prop, String NomAttribut, String DefautSiVide)
            {
                XmlNode node = Prop.Attributes[NomAttribut];
                if (node != null)
                {
                    String Val = node.Value;

                    if (String.IsNullOrWhiteSpace(Val))
                        return DefautSiVide;
                    else
                        return node.Value;
                }

                return "";
            };


            foreach (XmlNode Objet in xmlDoc.DocumentElement.SelectSingleNode("/Base/Objets").ChildNodes)
            {
                String pNomObjet = Objet.Attributes["name"].Value;

                Dictionary<String, Data> _DicPropriete = new Dictionary<String, Data>();

                foreach (XmlNode Propriete in Objet.ChildNodes)
                {
                    String pNomPropriete = Valeur(Propriete, "name", "");
                    _DicPropriete.Add(pNomPropriete, new Data(Propriete));
                }

                _DicIntituleType.Add(pNomObjet, new Data(Objet));
                _DicType.Add(pNomObjet, _DicPropriete);
            }

            _DicEnum = new Dictionary<string, Dictionary<int, string>>();

            foreach (XmlNode Enum in xmlDoc.DocumentElement.SelectSingleNode("/Base/Enums").ChildNodes)
            {
                String pNomEnum = Enum.Attributes["name"].Value;

                Dictionary<int, String> pEnum = new Dictionary<int, string>();

                foreach (XmlNode Champ in Enum.ChildNodes)
                {
                    int pValeur = (int)Convert.ChangeType(Champ.Attributes["valeur"].Value, typeof(int));
                    String pIntitule = Champ.Attributes["intitule"].Value;
                    pEnum.Add(pValeur, pIntitule);
                }

                _DicEnum.Add(pNomEnum, pEnum);
            }

            
        }

        private static Data Donnees(String Objet, String Propriete)
        {
            if (_DicType.ContainsKey(Objet))
            {
                Dictionary<String, Data> _DicPropriete = _DicType[Objet];
                if (_DicPropriete.ContainsKey(Propriete))
                {
                    return _DicPropriete[Propriete];
                }
            }

            return null;
        }

        public static String Intitule(String Objet, String Propriete)
        {
            Data D = Donnees(Objet, Propriete);
            if (D != null)
                return D.Intitule;

            // Si la cle n'existe pas, on renvoi une chaine formattée
            return "";
        }

        public static String IntituleType(String Objet)
        {

            if (_DicIntituleType.ContainsKey(Objet))
                return _DicIntituleType[Objet].Intitule;

            return Objet.UpperCaseFirstCharacter().Replace("_", " ");
        }

        public static Dictionary<String, String> DicEntete(String Objet)
        {
            Dictionary<String, String> pDic = new Dictionary<String, String>();
            if (!_DicType.ContainsKey(Objet))
                return null;

            Dictionary<String, Data> pDicType = _DicType[Objet];
            foreach (String Prop in pDicType.Keys)
                pDic.Add(Prop, pDicType[Prop].Entete);

            return pDic;
        }

        public static String Unite(String Objet, String Propriete)
        {
            Data D = Donnees(Objet, Propriete);
            if (D != null)
                return D.Unite;

            return "";
        }

        public static String Defaut(String Objet, String Propriete)
        {
            Data D = Donnees(Objet, Propriete);
            if (D != null)
                return D.Defaut;

            return "";
        }

        public static String Info(String Objet, String Propriete)
        {
            Data D = Donnees(Objet, Propriete);
            if (D != null)
                return D.Info;

            // Si la cle n'existe pas, on renvoi l'intitulé
            return Intitule(Objet, Propriete);
        }

        public static Dictionary<int, String> Enum(String Type)
        {
            if (_DicEnum.ContainsKey(Type))
                return _DicEnum[Type];

            return null;
        }

        private class Data
        {
            public String Intitule { get; set; }
            public String Unite { get; set; }
            public String Defaut { get; set; }
            public String Info { get; set; }
            public String Entete { get; set; }

            public Data(XmlNode Propriete)
            {
                // Fonction pour renvoyer dans tout les cas une chaine
                Func<String, String> Valeur = delegate(String NomAttribut)
                {
                    XmlNode node = Propriete.Attributes[NomAttribut];
                    if (node != null)
                        return node.Value;

                    return "";
                };

                String pNomPropriete = Valeur("name");
                Intitule = Valeur("intitule");
                Unite = Valeur("unite");
                Defaut = Valeur("defaut");
                Info = Valeur("info");
                Entete = Valeur("entete");

                if (String.IsNullOrWhiteSpace(Intitule))
                    Intitule = pNomPropriete.Replace("_", " ").UpperCaseFirstCharacter();

                // Si il n'y a pas d'info, on prend l'intitulé
                if (String.IsNullOrWhiteSpace(Info))
                    Info = Intitule;

                // Si il n'y a pas d'entete, on prend l'intitulé
                if (String.IsNullOrWhiteSpace(Entete))
                    Entete = Intitule;
            }
        }
    }
    
}

