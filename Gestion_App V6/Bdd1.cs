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
using System.Xml;

namespace GestionOld
{

    public static class Bdd1
    {
        private static Version VersionCourante = new Version(10);
        private static MySqlConnection _ConnexionBase = null;

        private static String FichierMapping = "MappingDesTables.xml";
        private static String FichierConnexion = "Connection.xml";

        public static String DB;
        public static String ConnexionCourante;

        //private static String _SvgNom = "SvgBase";
        //private static String _SvgExt = ".sql";

        static Bdd1() { }

        public static List<String> ListeBase()
        {
            List<String> ListeBases = new List<string>();

            String pChemin = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @FichierConnexion);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(pChemin);

            XmlNode Bases = xmlDoc.SelectSingleNode("Bases");

            if (Bases != null)
            {

                foreach (XmlNode nBase in Bases.ChildNodes)
                    ListeBases.Add(nBase.Attributes["name"].Value);
            }

            return ListeBases;
        }

        private static String ChargerInfosConnexion(XmlNode Connexion)
        {
            String Server = Connexion.SelectSingleNode("Server").InnerText;
            String Port = Connexion.SelectSingleNode("Port").InnerText;
            String User = Connexion.SelectSingleNode("User").InnerText;
            String Pw = Connexion.SelectSingleNode("Pw").InnerText;
            DB = Connexion.SelectSingleNode("DB").InnerText;

            ConnexionCourante = String.Format("{0} ({1})", Connexion.Attributes["name"].Value, Server);

            return String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4};", Server, Port, User, Pw, DB);
        }

        private static Boolean Connecter(String NomBase)
        {

            if ((_ConnexionBase != null) && (_ConnexionBase.State == ConnectionState.Open)) return true;

            String pChemin = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @FichierConnexion);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(pChemin);

            XmlNode Bases = xmlDoc.SelectSingleNode("Bases");

            if (Bases != null)
            {
                XmlNode Base = Bases.SelectSingleNode("Base[@name='" + NomBase + "']");

                foreach (XmlNode Connexion in Base.ChildNodes)
                {
                    
                    _ConnexionBase = new MySqlConnection(ChargerInfosConnexion(Connexion));

                    // Deux essais de connexion
                    for (int i = 0; i < 2; i++)
                    {
                        try
                        {
                            _ConnexionBase.Open();
                            break;
                        }
                        catch (Exception e)
                        {
                            Log.Methode("Bdd");
                            Log.Message(String.Format("Erreur de connection à la base de donnée : {0}", ConnexionCourante));
                            Log.Message(e);
                        }
                    }

                    if (_ConnexionBase.State == ConnectionState.Open)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void Deconnecter()
        {
            if (_ConnexionBase != null)
            {
                SauvegarderLaBase();
                _ConnexionBase.Close();
            }
        }

        public static Boolean Initialiser(String NomBase)
        {

            if (!Connecter(NomBase))
                return false;

            String pChemin = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @FichierMapping);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(pChemin);

            if(TableExiste(typeof(Version)))
            {

                ListeObservable<Version> ListeVersion = Liste<Version>();
                Version VersionBase = ListeVersion.OrderByDescending(i => i.No).First();

                if (VersionCourante.No > VersionBase.No)
                {
                    foreach (XmlNode Base in xmlDoc.SelectSingleNode("/Bases").ChildNodes)
                    {
                        int NoBase = (int)Convert.ChangeType(Base.Attributes["version"].Value, typeof(int));
                        if (ListeVersion.Count(x => x.No == NoBase) == 0)
                            MettreAJourLaBase(Base);
                    }

                    Bdd1.Ajouter<Version>(VersionCourante);
                    Bdd1.Enregistrer();
                }
            }
            else
            {
                foreach (XmlNode Base in xmlDoc.SelectSingleNode("/Bases").ChildNodes)
                {
                    MettreAJourLaBase(Base);
                }
                Bdd1.Ajouter<Version>(VersionCourante);
                Bdd1.Enregistrer();
            }
            return true;
        }

        private static void MettreAJourLaBase(XmlNode Base)
        {

            // On recupère la iste des tables
            List<String> pListeNomDesTables = NomDesTables();
            // On supprime les tables OLD_Table dans le cas ou il y a eu des plantages lors de la maj
            pListeNomDesTables.RemoveAll(t => Regex.IsMatch(t, "^" + OLD()));

            foreach (String Table in pListeNomDesTables)
            {
                // Si la OLD_Table n'existe pas, on renome la table
                if (!TableExiste(OLD(Table)))
                {
                    Executer(String.Format("ALTER TABLE {0} RENAME TO {1};", Table, OLD(Table)), null);
                }
            }

            // On récupère la liste des tables
            pListeNomDesTables = NomDesTables();

            foreach (Type Type in DicProprietes.ListeType())
            {
                #region MODIFICATION DE LA STRUCTURE

                CreerTable(Type);

                String pNomNouvelleTable = NomTable(Type);
                String pNomAncienneTable = OLD(pNomNouvelleTable);

                XmlNode pTable = Base.SelectSingleNode(String.Format("Structure/Tables/Table[@name='{0}']", pNomNouvelleTable));

                // Si un ancien nom est défini, on le récupère et on le formate
                if ((pTable != null) && (pTable.Attributes["oldname"] != null))
                    pNomAncienneTable = OLD(pTable.Attributes["oldname"].Value);

                // Si elle n'est pas dans la liste des tables, c'est une nouvelle table, donc on passe au Type suivant
                if (pListeNomDesTables.Contains(pNomAncienneTable))
                {

                    List<String> ListeAnciennesColonnes = NomDesColonnes(pNomAncienneTable);

                    List<String> pParamOldColonnes = new List<String>();
                    List<String> pParamNewColonnes = new List<String>();

                    foreach (PropertyInfo Prop in DicProprietes.ListePropriete(Type).Values)
                    {
                        String pNomNewColonne = NomChamp(Prop);
                        String pNomOldColonne = pNomNewColonne;

                        Boolean FusionDeColonnes;
                        FusionDeColonnes = false;

                        Boolean ValeurParDefaut;
                        ValeurParDefaut = false;

                        // Si il y a un parametrage pour la table, on va chercher le nom de la colonne
                        if (pTable != null)
                        {
                            XmlNode Colonne = pTable.SelectSingleNode(String.Format("Colonne[@name='{0}']", pNomNewColonne));

                            // Si il y a un parametrage "oldname" pour cette colonne, on recherche
                            if ((Colonne != null) && (Colonne.Attributes["oldname"] != null))
                                pNomOldColonne = Colonne.Attributes["oldname"].Value;

                            // Si il y a un parametrage "fusion" pour cette colonne, on recherche
                            if ((Colonne != null) && (Colonne.Attributes["fusion"] != null))
                            {
                                pNomOldColonne = Colonne.Attributes["fusion"].Value;
                                FusionDeColonnes = true;
                            }

                            // Si il y a un parametrage "defaut" pour cette colonne, on recherche
                            if ((Colonne != null) && (Colonne.Attributes["defaut"] != null))
                            {
                                pNomOldColonne = "(" + Colonne.Attributes["defaut"].Value + ") as " + pNomNewColonne;
                                ValeurParDefaut = true;
                            }

                        }

                        // Si la colonne existe dans l'ancienne table, on l'ajoute
                        if (ListeAnciennesColonnes.Contains(pNomOldColonne) || FusionDeColonnes || ValeurParDefaut)
                        {
                            if (Attribute.IsDefined(Prop, typeof(ClePrimaire)))
                            {
                                pParamOldColonnes.Insert(0, pNomOldColonne);
                                pParamNewColonnes.Insert(0, pNomNewColonne);
                            }
                            else
                            {
                                pParamOldColonnes.Add(pNomOldColonne);
                                pParamNewColonnes.Add(pNomNewColonne);
                            }
                        }
                    }

                    {
                        String pSql = String.Format("INSERT INTO {0} ({1}) SELECT {2} FROM {3};", pNomNouvelleTable, String.Join(" , ", pParamNewColonnes), String.Join(" , ", pParamOldColonnes), pNomAncienneTable);

                        Executer(pSql, null);
                    }

                    {
                        ListParametres pParams = new ListParametres();
                        pParams.Ajouter(new MySqlParameter(Prefix + "AncienneTable", DbType.String)).Value = pNomAncienneTable;

                        Executer(String.Format("DROP TABLE {0};", pNomAncienneTable), null);
                    }
                }
                #endregion
                #region INSERTION DES NOUVELLES VALEURS

                pTable = Base.SelectSingleNode(String.Format("Insertion/Tables/Table[@name='{0}']", pNomNouvelleTable));

                // Si la table existe, on insert les valeurs
                if (pTable != null)
                {
                    foreach (XmlNode Insert in pTable.ChildNodes)
                    {
                        if(Insert.Name == "Insert")
                        {

                            List<String> ListeColonne = new List<String>();
                            ListParametres pParams = new ListParametres();

                            foreach (XmlNode Valeur in Insert.ChildNodes)
                                if (Valeur.Name == "Valeur")
                                {
                                    ListeColonne.Add(Valeur.Attributes["name"].Value);
                                    pParams.Ajouter(new MySqlParameter(Prefix + Valeur.Attributes["name"].Value, Valeur.InnerText));
                                }


                            String pSql = String.Format("INSERT INTO {0} ( {1} ) VALUES ( {2} );", pNomNouvelleTable, String.Join(" , ", ListeColonne), String.Join(" , ", pParams.Noms));

                            Executer(pSql, pParams);
                        }
                    }
                }
                #endregion
            }
        }

        private static void SauvegarderLaBase()
        {
            //String codeBase = Assembly.GetExecutingAssembly().CodeBase;
            //UriBuilder uri = new UriBuilder(codeBase);
            //String CheminDossier = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));

            //// On supprime le précédent fichier
            //foreach (String F in Directory.GetFiles(CheminDossier, _SvgNom + "1" + _SvgExt, SearchOption.TopDirectoryOnly))
            //{
            //    File.Delete(F);
            //    break;
            //}

            //// On renomme le précédent fichier
            //foreach (String F in Directory.GetFiles(CheminDossier, _SvgNom + _SvgExt, SearchOption.TopDirectoryOnly))
            //{
            //    File.Move(F, Path.Combine(CheminDossier, _SvgNom + "1" + _SvgExt));
            //    break;
            //}

            //Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            //String CheminSvg = Path.Combine(CheminDossier, _SvgNom + _SvgExt);

            //string binary = @"C:\MySQL\MySQL Server 5.0\bin\mysqldump.exe";
            //string arguments = @"-uroot -ppassword sample";
            //ProcessStartInfo PSI = new System.Diagnostics.ProcessStartInfo(binary, arguments);
            //PSI.RedirectStandardInput = true;
            //PSI.RedirectStandardOutput = true;
            //PSI.RedirectStandardError = true;
            //PSI.UseShellExecute = false;
            //Process p = System.Diagnostics.Process.Start(PSI);
            //Encoding encoding = p.StandardOutput.CurrentEncoding;
            //System.IO.StreamWriter SW = new StreamWriter(@"c:\backup.sql", false, encoding);
            //p.WaitForExit();
            //string output = p.StandardOutput.ReadToEnd();
            //SW.Write(output);
            //SW.Close();
        }

        private static void CreerTable(Type T)
        {
            if (! TableExiste(T))
            {
                Dictionary<String, PropertyInfo> pDic = DicProprietes.ListePropriete(T);
                
                List<String> pDicColonnes = new List<String>();

                foreach (PropertyInfo Prop in pDic.Values)
                {
                    String Definition = "`" + NomChamp(Prop) + "` " + TypeChamp(Prop) + " " + ContrainteChamp(Prop);

                    if (Attribute.IsDefined(Prop, typeof(ClePrimaire)))
                        pDicColonnes.Insert(0, Definition);
                    else
                        pDicColonnes.Add(Definition);
                }

                pDicColonnes.Add("PRIMARY KEY (`" + NomChamp(DicProprietes.ClePrimaire(T)) + "`)");

                {
                    String Sql = String.Format("CREATE TABLE {0} ({1});", NomTable(T), String.Join(", ", pDicColonnes));

                    Executer(Sql, null);
                }

                return;
            }
        }

        private static Boolean TableExiste(Type T)
        {
            return TableExiste(NomTable(T));
        }

        private static Boolean TableExiste(String NomTable)
        {
            List<String> pListeNoms = NomDesTables();
            if (pListeNoms.Contains(NomTable))
                return true;
            else
                return false;
        }

        private static List<String> NomDesTables()
        {
            List<String> Liste = new List<string>();
            DataTable InfoTable = RecupererTable(String.Format("SELECT table_name FROM information_schema.tables WHERE table_schema='{0}';", DB));

            foreach (DataRow R in InfoTable.Rows)
                Liste.Add(R["table_name"].ToString());

            return Liste;
        }

        private static List<String> NomDesColonnes(Type T)
        {
            return NomDesColonnes(NomTable(T));
        }

        private static List<String> NomDesColonnes(String NomTable)
        {
            List<String> Liste = new List<string>();
            DataTable InfoTable = RecupererTable(String.Format("SELECT column_name FROM information_schema.columns WHERE table_name = '{0}'", NomTable));

            foreach (DataRow R in InfoTable.Rows)
                Liste.Add(R["column_name"].ToString());

            return Liste;
        }

        private static DataTable RecupererTable(String sql)
        {
            DbDataReader Lecteur = null;
            try
            {
                DataTable dt = new DataTable();
                MySqlCommand Cmde = new MySqlCommand(sql, _ConnexionBase);
                Lecteur = Cmde.ExecuteReader();
                dt.Load(Lecteur);
                Lecteur.Close();
                return dt;
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                if (Lecteur != null)
                    Lecteur.Close();

                return null;
            }
        }

        private static async Task<DataTable> RecupererTableAsync(String sql)
        {
            DbDataReader Lecteur = null;
            try
            {
                DataTable dt = new DataTable();
                MySqlCommand Cmde = new MySqlCommand(sql, _ConnexionBase);
                Lecteur = await Cmde.ExecuteReaderAsync();
                dt.Load(Lecteur);
                Lecteur.Close();
                return dt;
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                if (Lecteur != null)
                    Lecteur.Close();

                return null;
            }
        }

        private static Object RecupererChamp(String sql, Type TypeValeur)
        {
            MySqlCommand Cmde = new MySqlCommand(sql, _ConnexionBase);
            object Valeur = Cmde.ExecuteScalar();
            Cmde.Dispose();

            if ((Valeur == null) || (Valeur == System.DBNull.Value))
            {    
                return TypeValeur.GetDefaultValue();
            }
            
            return Convert.ChangeType(Valeur, TypeValeur);
        }

        private static async Task<Object> RecupererChampAsync(String sql, Type TypeValeur)
        {
            MySqlCommand Cmde = new MySqlCommand(sql, _ConnexionBase);
            object Valeur = await Cmde.ExecuteScalarAsync();
            if ((Valeur == null) || (Valeur == System.DBNull.Value))
            {
                return TypeValeur.GetDefaultValue();
            }

            return Convert.ChangeType(Valeur, TypeValeur);
        }

        private static async void ExecuterAsync(String sql, ListParametres Liste)
        {
            MySqlCommand Cmde = new MySqlCommand(sql, _ConnexionBase);

            Cmde.Parameters.Clear();

            if (Liste != null)
                foreach (MySqlParameter Item in Liste)
                    Cmde.Parameters.Add(Item);

            int Ligne = await Cmde.ExecuteNonQueryAsync();

            Cmde.Dispose();
        }

        private static void Executer(String sql, ListParametres Liste)
        {
            MySqlCommand Cmde = new MySqlCommand(sql, _ConnexionBase);

            Cmde.Parameters.Clear();

            if (Liste != null)
                foreach (MySqlParameter Item in Liste)
                    Cmde.Parameters.Add(Item);

            Cmde.ExecuteNonQuery();

            Cmde.Dispose();
        }

        private static Boolean IsOLD(String NomTable)
        {
            return Regex.IsMatch(NomTable, "^" + OLD());
        }

        private static String OLD()
        {
            return "old_";
        }
        private static String OLD(String NomTable)
        {
            return OLD() + NomTable;
        }
        private static String NomTable(Type T)
        {
            return T.Name.ToString().ToLowerInvariant();
        }
        private static String NomClePrimaire(Type T)
        {
            return NomChamp(DicProprietes.ClePrimaire(T));
        }
        private static String NomChamp(Type T)
        {
            return NomChamp(T.Name);
        }
        private static String NomChamp(PropertyInfo P)
        {
            return NomChamp(P.Name);
        }
        private static String NomChamp(String Nom)
        {
            return Nom.ToLowerInvariant();
        }
        private static String DirectionTri(ListSortDirection Dir)
        {
            switch (Dir)
            {
                case ListSortDirection.Ascending:
                    return "ASC";
                case ListSortDirection.Descending:
                    return "DESC";
                default:
                    return "ASC";
            }
        }
        private static List<String> NomClesTri(Type T)
        {
            List<String> NomCles = new List<String>();
            List<PropertyInfo> pListeTri = DicProprietes.ListeTri(T);

            // On récupère le nom du champ
            NomCles = pListeTri.Select(x => NomChamp(x) + 
                                            " " +
                                            DirectionTri((x.GetCustomAttributes(typeof(Tri)).First() as Tri).DirectionTri)
                                            ).ToList<String>();

            if(NomCles.Count == 0)
                NomCles.Add(NomClePrimaire(T) + " ASC");

            return NomCles;
        }
        private static String TypeChamp(PropertyInfo P)
        {
            Propriete pAttProp = (Propriete)P.GetCustomAttributes(typeof(Propriete)).First();

            switch (pAttProp.Type)
            {
                case TypeSQL_e.cAuto:
                    if (P.PropertyType == typeof(int))
                        return "INTEGER";
                    else if (P.PropertyType == typeof(Boolean))
                        return "INTEGER";
                    else if (P.PropertyType == typeof(Double))
                        return "DOUBLE";
                    else if (P.PropertyType == typeof(DateTime))
                        return "TIMESTAMP";
                    else if (P.PropertyType == typeof(String))
                        return "TEXT";
                    else if (P.PropertyType.IsEnum)
                        return "INTEGER";
                    else
                        return "TEXT";

                case TypeSQL_e.cInt:
                    return "INTEGER";

                case TypeSQL_e.cBool:
                    return "INTEGER";

                case TypeSQL_e.cDbl:
                    return "DOUBLE";

                case TypeSQL_e.cDate:
                    return "TEXT";

                case TypeSQL_e.cString:
                    return "TEXT";

                case TypeSQL_e.cEnum:
                    return "INTEGER";
                
                case TypeSQL_e.cSerial:
                    return "INTEGER";

                default:
                    return "TEXT";
            }
        }
        private static String ContrainteChamp(PropertyInfo P)
        {
            Propriete pAttProp = P.GetCustomAttributes(typeof(Propriete)).First() as Propriete;

            if(pAttProp != null)
                return pAttProp.Contrainte;

            return "";
        }
        
        // Renvoi l'id d'une ligne
        private static int IdLigne(DataRow Li, Type T)
        {
            return (int)Convert.ChangeType(Li[NomClePrimaire(T)], typeof(int));
        }

        private static int DernierIdInsere(Type T)
        {
            MySqlCommand Cmde = new MySqlCommand("SELECT last_insert_id()", _ConnexionBase);
            object Valeur = Cmde.ExecuteScalar();
            try
            {
                return (int)Convert.ChangeType(Valeur, typeof(int));
            }
            catch
            {
                return 0;
            }
        }

        // Pour eviter les callbacks en boucle avec le binding WPF, il faut les shunter lors de l'initialisation de l'objet
        private static Boolean ModeChargerObjet = false;
        private static Boolean ModeAjouterObjet = false;

        private static T ChargerObjet<T>(DataRow Li)
            where T : ObjetGestion, new()
        {
            return ChargerObjet<T, T>(Li, null);
        }

        private static T ChargerObjet<T, U>(DataRow Li, U Parent)
            where T : ObjetGestion, new()
            where U : ObjetGestion
        {
            if (ModeChargerObjet) return null;

            ModeChargerObjet = true;

            // On récupère l'objet dans le dico
            T Objet = DicObjet.RecupererObjet<T>(IdLigne(Li, typeof(T)));

            // S'il n'existe pas, on le crée et on le rempli
            if (Objet == null)
            {
                Objet = new T();

                // On récupère les propriétés
                Dictionary<String, PropertyInfo> pDicProp = DicProprietes.ListePropriete(typeof(T));

                // On charge la cle primaire en premier
                PropertyInfo ClePrimaire = DicProprietes.ClePrimaire(typeof(T));
                {
                    String pNomChamp = NomChamp(ClePrimaire);

                    if (Li.Table.Columns.Contains(pNomChamp))
                    {
                        if (System.DBNull.Value == Li[pNomChamp])
                            return null;

                        ClePrimaire.SetValue(Objet, Convert.ChangeType(Li[pNomChamp], ClePrimaire.PropertyType));
                    }
                }

                // Pour chacune d'elle, on les initialise correctement avec les valeurs de la base
                foreach (PropertyInfo Prop in pDicProp.Values)
                {
                    String pNomChamp = NomChamp(Prop);

                    if (Attribute.IsDefined(Prop, typeof(CleEtrangere)))
                    {
                        if ((Parent != null) && (Prop.PropertyType == typeof(U)))
                            Prop.SetValue(Objet, Parent);
                    }
                    else if (Li.Table.Columns.Contains(pNomChamp))
                    {
                        if (System.DBNull.Value == Li[pNomChamp])
                            continue;

                        if (Prop.PropertyType.IsEnum)
                            Prop.SetValue(Objet, System.Enum.Parse(Prop.PropertyType, Li[pNomChamp].ToString()));
                        else
                            Prop.SetValue(Objet, Convert.ChangeType(Li[pNomChamp], Prop.PropertyType));
                    }
                }

                DicObjet.ReferencerObjet(Objet, typeof(T));
            }

            Objet.EstCharge = true;

            ModeChargerObjet = false;

            return Objet;
        }

        public static T Parent<T, U>(U Enfant)
            where T : ObjetGestion, new()
            where U : ObjetGestion
        {
            // Si on demande les enfants d'un Objet non sauvegardé dans la base, il y a un pb
            // on renvoi la liste vide.
            if ((Enfant != null) && (!Enfant.EstSvgDansLaBase))
            {
                Log.Methode("Bdd");
                Log.Write("Erreur ID = -1");
                Log.Write("Id : " + Enfant.Id);
                return null;
            }

            String pQuery = String.Format("SELECT {0} FROM {1} WHERE {2} = {3};", NomChamp(typeof(T)), NomTable(typeof(U)), NomClePrimaire(typeof(U)), Enfant.Id);
            int pId = (int)Convert.ChangeType(RecupererChamp(pQuery, typeof(int)), typeof(int));

            pQuery = String.Format("SELECT * FROM {0} WHERE {1} = {2};", NomTable(typeof(T)), NomClePrimaire(typeof(T)), pId);
            DataTable Table = RecupererTable(pQuery);

            if ((Table == null) || (Table.Rows.Count == 0))
                return null;

            return ChargerObjet<T>(Table.Rows[0]);
        }

        public static ListeObservable<T> Liste<T>()
            where T : ObjetGestion, new()
        {
            if (ModeChargerObjet || ModeAjouterObjet) return null;

            ListeObservable<T> pListe = DicObjet.RecupererListe<T>();

            if (pListe == null)
            {
                pListe = Enfants<T, T>(null);
                DicObjet.ReferencerListe(pListe, typeof(T));
            }

            return pListe;
        }

        public static ListeObservable<T> Enfants<T, U>(U Parent)
            where T : ObjetGestion, new()
            where U : ObjetGestion
        {

            if (ModeChargerObjet || ModeAjouterObjet) return null;

            ListeObservable<T> pListe = new ListeObservable<T>();

            String pQuery = String.Format("SELECT * FROM {0}", NomTable(typeof(T)));

            // Si on demande les enfants d'un Objet non sauvegardé dans la base, il y a un pb
            // on renvoi la liste vide.
            if ((Parent != null) && (!Parent.EstSvgDansLaBase))
            {
                
                Log.Methode("Bdd");
                Log.Write("Erreur ID = -1");
                Log.Write("Id : " + Parent.Id);
                return pListe;
            }

            // Filtre sur un objet
            if (Parent != null)
                pQuery += String.Format(" WHERE {0} = {1}", NomChamp(Parent.GetType()), Parent.Id);

            // S'il y a des clef de tri, on les ajoute
            List<String> pNomClesTri = NomClesTri(typeof(T));
            if (pNomClesTri.Count > 0)
                pQuery += String.Format(" ORDER BY {0};", String.Join(", ", pNomClesTri));

            DataTable Table = RecupererTable(pQuery);

            // Si la requete est nulle, on renvoi une liste vide
            if (Table == null) return pListe;

            // Sinon, on charge les objets
            foreach (DataRow Li in Table.Rows)
            {
                T pObj = ChargerObjet<T, U>(Li, Parent);
                pListe.Add(pObj);
            }

            return pListe;
        }

        public static void Ajouter<T>(T Objet)
            where T : ObjetGestion
        {
            if (ModeChargerObjet) return;

            ModeAjouterObjet = true;

            Dictionary<String, PropertyInfo> pDicProp = DicProprietes.ListePropriete(typeof(T));

            foreach (PropertyInfo Prop in pDicProp.Values)
            {
                String Defaut = DicIntitules.Defaut(typeof(T).Name, Prop.Name);

                #region CLE PRIMAIRE
                // Si c'est une clé primaire, on passe à la suivante
                if (Attribute.IsDefined(Prop, typeof(ClePrimaire)))
                    continue;

                #endregion

                #region CLE ETRANGERE
                if (Attribute.IsDefined(Prop, typeof(CleEtrangere)))
                    continue;

                #endregion

                #region INTEGER MAX
                // Si c'est une propriété MAX, on récupère le maximum de la colonne suivant les conditions
                if (Attribute.IsDefined(Prop, typeof(Max)))
                {
                    String pQueryMax = String.Format("SELECT MAX({0}) FROM {1}", NomChamp(Prop), NomTable(typeof(T)));

                    int pVal = (int)RecupererChamp(pQueryMax, Prop.PropertyType) + 1;
                    Prop.SetValue(Objet, pVal);
                    continue;
                }
                #endregion

                if (Prop.PropertyType.IsEnum && !String.IsNullOrWhiteSpace(Defaut))
                {
                    Prop.SetValue(Objet, System.Enum.Parse(Prop.PropertyType, Defaut));
                    continue;
                }

                // Si la valeur par défaut n'est pas vide
                if (!String.IsNullOrWhiteSpace(Defaut))
                {
                    Prop.SetValue(Objet, Convert.ChangeType(Defaut, Prop.PropertyType));
                    continue;
                }

                // Si la valeur de la propriete est null
                if (Prop.GetValue(Objet) == null)
                {
                    Prop.SetValue(Objet, Prop.PropertyType.GetDefaultValue());
                    continue;
                }

                // Sinon on laisse la valeur existante
            }

            if (Attribute.IsDefined(typeof(T), typeof(ForcerAjout)))
                Ajouter(Objet, typeof(T));
            else
                ListeAjouter.Ajouter(Objet, typeof(T));

            Objet.EstCharge = true;

            ModeAjouterObjet = false;
        }

        public static void Maj(ObjetGestion Objet, Type T, String NomPropriete = null)
        {
            if (ModeChargerObjet || ModeAjouterObjet) return;

            ListeMaj.Ajouter(Objet, T);
        }

        public static void Supprimer<T>(T Objet)
            where T : ObjetGestion
        {
            if (Objet.EstSvgDansLaBase)
            {
                ListeAjouter.Supprimer(Objet);
                ListeMaj.Supprimer(Objet);
                ListeSupprimer.Ajouter(Objet, typeof(T));
            }

            Objet = null;
        }

        private class GenericOrderedDictionary<T, U> : OrderedDictionary
        {
            public void Ajouter(T key, U value)
            {
                if (!Contains(key))
                    base.Add(key, value);
            }

            public void Supprimer(T key)
            {
                base.Remove(key);
            }
        }

        private class DicObjetBdd : GenericOrderedDictionary<ObjetGestion, Type> { }

        private static DicObjetBdd ListeAjouter = new DicObjetBdd();
        private static DicObjetBdd ListeMaj = new DicObjetBdd();
        private static DicObjetBdd ListeSupprimer = new DicObjetBdd();

        public static Boolean DoitEtreEnregistre
        {
            get
            {
                if ((ListeAjouter.Count + ListeMaj.Count + ListeSupprimer.Count) > 0)
                    return true;

                return false;
            }
        }

        public static void Enregistrer()
        {
            Log.Methode("Bdd");

            foreach (DictionaryEntry v in ListeAjouter)
                Ajouter((ObjetGestion)v.Key, (Type)v.Value);

            foreach (DictionaryEntry v in ListeMaj)
                Maj((ObjetGestion)v.Key, (Type)v.Value);

            foreach (DictionaryEntry v in ListeSupprimer)
                Supprimer((ObjetGestion)v.Key, (Type)v.Value);

            ListeAjouter.Clear();
            ListeMaj.Clear();
            ListeSupprimer.Clear();
        }

        private static String Prefix = "@";
        private static readonly String PrefixClef = Prefix + "Valeur_";

        private static void Ajouter(ObjetGestion Objet, Type T)
        {
            if (Objet.EstSvgDansLaBase) return;

            Dictionary<String, PropertyInfo> pDicProp = DicProprietes.ListePropriete(T);

            List<String> pListeChamps = new List<String>();
            ListParametres pDicValeurs = new ListParametres();

            foreach (PropertyInfo Prop in pDicProp.Values)
            {
                String Defaut = DicIntitules.Defaut(T.Name, Prop.Name);

                String Cle = PrefixClef + NomChamp(Prop);

                #region CLE PRIMAIRE
                // Si c'est une clé primaire, on passe à la suivante
                if (Attribute.IsDefined(Prop, typeof(ClePrimaire)))
                    continue;

                #endregion

                #region CLE ETRANGERE
                if (Attribute.IsDefined(Prop, typeof(CleEtrangere)))
                {
                    // On récupère l'id de l'objet
                    Object Obj = Prop.GetValue(Objet);

                    // S'il est égale à la valeur par défaut, il est initialisé donc on l'ajoute
                    if (Obj != null)
                    {
                        pListeChamps.Add(NomChamp(Prop));
                        pDicValeurs.Ajouter(new MySqlParameter(Cle, MySqlDbType.Int32)).Value = Convert.ToInt32(Obj.ToString());
                    }
                    continue;
                }

                #endregion

                pListeChamps.Add(NomChamp(Prop));

                // Delegate pour l'insertion dans le dictionnaire
                Action<MySqlDbType, Func<Object, Object>> AjouterDic = delegate(MySqlDbType t, Func<Object, Object> f)
                {
                    Func<Object, Func<Object, Object>, Object> Valeur = delegate(Object v, Func<Object, Object> c)
                    {
                        if (c == null)
                            return v;

                        return c(v);
                    };
                    
                    if (Prop.GetValue(Objet) != null)
                        pDicValeurs.Ajouter(new MySqlParameter(Cle, t)).Value = Valeur(Prop.GetValue(Objet), f);
                    else if (String.IsNullOrWhiteSpace(Defaut))
                        pDicValeurs.Ajouter(new MySqlParameter(Cle, t)).Value = Valeur(Prop.PropertyType.GetDefaultValue(), f);
                    else
                        pDicValeurs.Ajouter(new MySqlParameter(Cle, t)).Value = Convert.ChangeType(Valeur(Defaut, f), Prop.PropertyType);
                };


                #region ENUM
                // Si c'est un Enum, on fait la conversion
                if (Prop.PropertyType.IsEnum)
                {
                    AjouterDic(MySqlDbType.Int32, o => {
                        String s = o as String;
                        if(s != null)
                            o = System.Enum.Parse(Prop.PropertyType, s);

                        return Convert.ToInt32(o);
                    });
                    continue;
                }
                #endregion

                #region BOOLEAN
                // Si c'est un Boolean, c'est pareil, on converti
                if (Prop.PropertyType == typeof(Boolean))
                {
                    AjouterDic(MySqlDbType.Int32, o => { return Convert.ToInt32(o); });
                    continue;
                }
                #endregion

                #region STRING
                // Si c'est un String, c'est pareil, on converti
                if (Prop.PropertyType == typeof(String))
                {
                    AjouterDic(MySqlDbType.Text, null);
                    continue;
                }
                #endregion

                #region DATETIME
                // Si c'est un DateTime, c'est pareil, on converti
                if (Prop.PropertyType == typeof(DateTime))
                {
                    AjouterDic(MySqlDbType.Timestamp, null);
                    continue;
                }
                #endregion

                #region DOUBLE
                // Si c'est un Double, c'est pareil, on converti
                if (Prop.PropertyType == typeof(Double))
                {
                    AjouterDic(MySqlDbType.Double, o =>
                    {
                        Double s = (Double)o;
                        if (Double.IsNaN(s))
                            return 0;

                        return o;
                    });
                    continue;
                }
                #endregion

                #region INTEGER
                // Si c'est un int32, c'est pareil, on converti
                if (Prop.PropertyType == typeof(int))
                {
                    AjouterDic(MySqlDbType.Int32, null);
                    continue;
                }
                #endregion

                if (Prop.GetValue(Objet) != null)
                    pDicValeurs.Ajouter(new MySqlParameter(Cle, Prop.GetValue(Objet).ToString()));
                else if (String.IsNullOrWhiteSpace(Defaut))
                    pDicValeurs.Ajouter(new MySqlParameter(Cle, Prop.PropertyType.GetDefaultValue()));
                else
                    pDicValeurs.Ajouter(new MySqlParameter(Cle, Convert.ChangeType(Defaut, Prop.PropertyType)));
            }

            {
                String pQuery = String.Format("INSERT INTO {0} ( {1} ) VALUES ( {2} );",
                    NomTable(T),
                    String.Join(" , ", pListeChamps),
                    String.Join(" , ", pDicValeurs.Noms)
                    );
                
                ExecuterAsync(pQuery, pDicValeurs);
            }

            Objet.Id = DernierIdInsere(T);

            DicObjet.ReferencerObjet(Objet, T);
        }

        private static void Maj(ObjetGestion Objet, Type T)
        {
            if (!Objet.EstSvgDansLaBase) return;

            Dictionary<String, PropertyInfo> pDicProp = DicProprietes.ListePropriete(T);

            List<PropertyInfo> ListeProp = new List<PropertyInfo>();

            ListeProp = pDicProp.Values.ToList<PropertyInfo>();

            ListParametres pDicValeurs = new ListParametres();
            List<String> pListChaine = new List<String>();

            foreach (PropertyInfo Prop in ListeProp)
            {
                if ((!Attribute.IsDefined(Prop, typeof(ClePrimaire))) && (Prop.GetValue(Objet) != null))
                {
                    String Cle = PrefixClef + NomChamp(Prop);

                    pListChaine.Add(NomChamp(Prop) + " = " + Cle);

                    if (Attribute.IsDefined(Prop, typeof(CleEtrangere)))
                    {
                        // On récupère l'id de l'objet
                        Object Obj = Prop.GetValue(Objet);

                        // S'il est égale à la valeur par défaut, il est initialisé donc on l'ajoute
                        if (Obj != null)
                            pDicValeurs.Ajouter(new MySqlParameter(Cle, MySqlDbType.Int32)).Value = Convert.ToInt32(Obj.ToString());
                        continue;
                    }
                    else if (Prop.PropertyType.IsEnum)
                    {
                        pDicValeurs.Ajouter(new MySqlParameter(Cle, MySqlDbType.Int32)).Value = (int)Prop.GetValue(Objet);
                    }
                    else if (Prop.PropertyType == typeof(int))
                    {
                        pDicValeurs.Ajouter(new MySqlParameter(Cle, MySqlDbType.Int32)).Value = Prop.GetValue(Objet);
                    }
                    else if (Prop.PropertyType == typeof(Boolean))
                    {
                        pDicValeurs.Ajouter(new MySqlParameter(Cle, MySqlDbType.Int32)).Value = Convert.ToInt32(Prop.GetValue(Objet));
                    }
                    else if (Prop.PropertyType == typeof(Double))
                    {
                        Double Val = (Double)Prop.GetValue(Objet);
                        if (Double.IsNaN(Val))
                            Val = 0;

                        pDicValeurs.Ajouter(new MySqlParameter(Cle, MySqlDbType.Double)).Value = Val;
                    }
                    else if (Prop.PropertyType == typeof(DateTime))
                    {
                        pDicValeurs.Ajouter(new MySqlParameter(Cle, MySqlDbType.Timestamp)).Value = Prop.GetValue(Objet);
                    }
                    else if (Prop.PropertyType == typeof(String))
                    {
                        pDicValeurs.Ajouter(new MySqlParameter(Cle, MySqlDbType.Text)).Value = Prop.GetValue(Objet);
                    }
                    else
                    {
                        pDicValeurs.Ajouter(new MySqlParameter(Cle, Prop.GetValue(Objet)));
                    }
                }
            }

            pDicValeurs.Ajouter(new MySqlParameter(PrefixClef + NomClePrimaire(T), Objet.Id));

            String pQuery = String.Format("UPDATE {0} SET {1} WHERE {2} = {3}", NomTable(T), String.Join(" , ", pListChaine), NomClePrimaire(T), PrefixClef + NomClePrimaire(T));
            ExecuterAsync(pQuery, pDicValeurs);
        }

        private static void Supprimer(ObjetGestion Objet, Type T)
        {
            if (!Objet.EstSvgDansLaBase) return;

            DicObjet.SupprimerObjet(Objet, T);

            String pQuery = String.Format("DELETE FROM {0} WHERE {1} = {2};", NomTable(T), NomClePrimaire(T), Objet.Id.ToString());
            ExecuterAsync(pQuery, null);
        }

        #region ANALYSE
        private static String AffNb(Object Obj, String Unite = "")
        {
            return (Math.Round(((Double)Convert.ChangeType(Obj, typeof(Double)))).ToString() + " " + Unite).Trim();
        }

        //public static void AnalyseDevis(ref ListeAvecTitre<Object> ListeCode, ref ListeAvecTitre<Object> ListeFamille, int Id)
        //{
//            Dictionary<int, CodeFamille_e> pDicCode = new Dictionary<int, CodeFamille_e>();
//            foreach (CodeFamille_e Code in Enum.GetValues(typeof(CodeFamille_e)))
//            {
//                if (!String.IsNullOrWhiteSpace(Code.GetEnumDescription()))
//                    pDicCode.Add((int)Code, Code);
//            }

//            Dictionary<int, Famille> pDicFamille = new Dictionary<int, Famille>();
//            foreach (Famille Famille in Bdd.Liste<Famille>())
//            {
//                if(pDicCode.ContainsValue(Famille.Code))
//                    pDicFamille.Add(Famille.Id, Famille);
//            }

//            {
//                String sql = String.Format(@"SELECT tmp.famille, tmp.qte, tmp.tt 
//                                             FROM 
//                                                (SELECT poste.devis,
//                                                        ligne_poste.famille,
//                                                        sum(CASE ligne_poste.prix_forfaitaire WHEN true THEN ligne_poste.qte ELSE ligne_poste.qte * poste.qte END) AS qte,
//                                                        sum(ligne_poste.debours_unitaire * poste.qte) AS tt
//                                                FROM ligne_poste
//                                                JOIN poste
//                                                ON ligne_poste.poste = poste.id
//                                                WHERE ligne_poste.statut = true AND poste.statut = true
//                                                GROUP BY poste.devis, ligne_poste.famille) AS tmp
//                                            JOIN devis
//                                            ON tmp.devis = devis.id
//                                            WHERE devis.id = {0}
//                                            ORDER BY tmp.famille;"
//                                              , Id.ToString());
//                DataTable Table = RecupererTable(sql);
//                if (Table == null) return;
//                foreach (DataRow Li in Table.Rows)
//                {
//                    int Index = (int)Convert.ChangeType(Li["famille"], typeof(int));

//                    if (!pDicFamille.ContainsKey(Index))
//                        continue;
                    
//                    Famille F = pDicFamille[Index];
                    
//                    ListeFamille.Add(new { Intitule = F.Description,
//                                            Qte = AffNb(Li["qte"], F.Unite),
//                                            Tt = AffNb(Li["tt"], "€")
//                    });
//                }
//            }

//            {
//                String sql = String.Format(@"CREATE TEMPORARY TABLE tmp AS
//                                             SELECT tmp.famille, tmp.qte, tmp.tt 
//                                             FROM 
//                                                (SELECT poste.devis,
//                                                        ligne_poste.famille,
//                                                        sum(CASE ligne_poste.prix_forfaitaire WHEN true THEN ligne_poste.qte ELSE ligne_poste.qte * poste.qte END) AS qte,
//                                                        sum(ligne_poste.debours_unitaire * poste.qte) AS tt
//                                                FROM ligne_poste
//                                                JOIN poste
//                                                ON ligne_poste.poste = poste.id
//                                                WHERE ligne_poste.statut = true AND poste.statut = true
//                                                GROUP BY poste.devis, ligne_poste.famille) AS tmp
//                                            JOIN devis
//                                            ON tmp.devis = devis.id
//                                            WHERE devis.id = {0}
//                                            ORDER BY tmp.famille;
//
//                                            SELECT famille.code,
//                                                   sum(tmp.qte) AS qte,
//                                                   sum(tmp.tt) AS tt
//                                            FROM tmp 
//                                            JOIN famille
//                                            ON tmp.famille = famille.id
//                                            GROUP BY famille.code
//                                            ORDER BY famille.code;
//                                            
//                                            DROP TEMPORARY TABLE IF EXISTS tmp;"
//                                              , Id.ToString());
//                DataTable Table = RecupererTable(sql);
//                if (Table == null) return;
//                foreach (DataRow Li in Table.Rows)
//                {
//                    int Index = (int)Convert.ChangeType(Li["code"], typeof(int));
//                    if (!pDicCode.ContainsKey(Index)) continue;
//                    CodeFamille_e C = pDicCode[Index];
//                    ListeCode.Add(new
//                    {
//                        Intitule = C.GetEnumDescription(),
//                        Qte = AffNb(Li["qte"].ToString(), C.GetEnumUnite()),
//                        Tt = AffNb(Li["tt"], "€")
//                    });
//                }
//            }
        //}

        public static void AnalyseClient(ref ListeAvecTitre<ListeAvecTitre<Object>> ListeAnalyseDevis, ref ListeAvecTitre<ListeAvecTitre<Object>> ListeAnalyseFacture, int Id)
        {
            #region DEVIS

            Dictionary<int, StatutDevis_e> pDicStatutDevis = new Dictionary<int, StatutDevis_e>();
            foreach (StatutDevis_e Statut in Enum.GetValues(typeof(StatutDevis_e)))
                pDicStatutDevis.Add((int)Statut, Statut);
            
            {
                String sql = String.Format(@"SELECT statut,
                                                    count(id) as nb,
                                                    sum(prix_ht) as ht,
                                                    sum(marge) as marge,
                                                    sum(prix_tt_achat) as achat,
                                                    sum(deja_facture_ht) as deja_facture,
                                                    sum(reste_a_facture_ht) as reste_facture,
                                                    year(date) AS dte
                                             FROM devis
                                             WHERE client = {0}
                                             GROUP BY statut, dte
                                             ORDER BY dte DESC, statut;"
                                              , Id.ToString());
                DataTable Table = RecupererTable(sql);
                if (Table == null) return;

                Dictionary<int, ListeAvecTitre<Object>> pDic = new Dictionary<int, ListeAvecTitre<Object>>();
                foreach (DataRow Li in Table.Rows)
                {
                    int Index = (int)Convert.ChangeType(Li["statut"], typeof(int));

                    if (!pDicStatutDevis.ContainsKey(Index))
                        continue;

                    StatutDevis_e D = pDicStatutDevis[Index];

                    int Date = (int)Convert.ChangeType(Li["dte"], typeof(int));
                    ListeAvecTitre<Object> pListe = null;

                    if (pDic.ContainsKey(Date))
                        pListe = pDic[Date];
                    else
                    {
                        pListe = new ListeAvecTitre<Object>(Date.ToString());
                        pDic.Add(Date, pListe);
                    }

                    pListe.Add(new
                    {
                        Intitule = D.GetEnumDescription(),
                        Nb = AffNb(Li["nb"]),
                        Ht = AffNb(Li["ht"], "€"),
                        Marge = AffNb(Li["marge"], "€"),
                        Achat = AffNb(Li["achat"], "€"),
                        Deja_Facture = AffNb(Li["deja_facture"], "€"),
                        Reste_Facture = AffNb(Li["reste_facture"], "€")
                    });
                }

                ListeAnalyseDevis.Importer(pDic.Values);
            }

            #endregion

            #region FACTURE

            Dictionary<int, StatutFacture_e> pDicStatutFacture = new Dictionary<int, StatutFacture_e>();
            foreach (StatutFacture_e Statut in Enum.GetValues(typeof(StatutFacture_e)))
                pDicStatutFacture.Add((int)Statut, Statut);

            {
                String sql = String.Format(@"SELECT facture.statut,
                                                    count(facture.id) as nb,
                                                    sum(facture.prix_ht) as ht,
                                                    year(facture.date) AS dte
                                             FROM facture
                                             INNER JOIN devis
                                             ON facture.devis = devis.id
                                             WHERE devis.client = {0}
                                             GROUP BY facture.statut, dte
                                             ORDER BY dte DESC, facture.statut;"
                                              , Id.ToString());
                DataTable Table = RecupererTable(sql);
                if (Table == null) return;

                Dictionary<int, ListeAvecTitre<Object>> pDic = new Dictionary<int, ListeAvecTitre<Object>>();
                foreach (DataRow Li in Table.Rows)
                {
                    int Index = (int)Convert.ChangeType(Li["statut"], typeof(int));

                    if (!pDicStatutFacture.ContainsKey(Index))
                        continue;
                    
                    StatutFacture_e F = pDicStatutFacture[Index];

                    int Date = (int)Convert.ChangeType(Li["dte"], typeof(int));
                    ListeAvecTitre<Object> pListe = null;

                    if (pDic.ContainsKey(Date))
                        pListe = pDic[Date];
                    else
                    {
                        pListe = new ListeAvecTitre<Object>(Date.ToString());
                        pDic.Add(Date, pListe);
                    }

                    pListe.Add(new
                    {
                        Intitule = F.GetEnumDescription(),
                        Nb = AffNb(Li["nb"]),
                        Ht = AffNb(Li["ht"], "€")
                    });
                }

                ListeAnalyseFacture.Importer(pDic.Values);
            }

            #endregion
        }

        public static void AnalyseSociete(ref ListeAvecTitre<ListeAvecTitre<Object>> ListeAnalyseDevis, ref ListeAvecTitre<ListeAvecTitre<Object>> ListeAnalyseFacture, int Id)
        {
            #region DEVIS

            Dictionary<int, StatutDevis_e> pDicStatutDevis = new Dictionary<int, StatutDevis_e>();
            foreach (StatutDevis_e Statut in Enum.GetValues(typeof(StatutDevis_e)))
                pDicStatutDevis.Add((int)Statut, Statut);

            {
                String sql = String.Format(@"SELECT devis.statut,
                                                    count(devis.id) as nb,
                                                    sum(devis.prix_ht) as ht,
                                                    sum(devis.marge) as marge,
                                                    sum(devis.prix_tt_achat) as achat,
                                                    sum(devis.deja_facture_ht) as deja_facture,
                                                    sum(devis.reste_a_facture_ht) as reste_facture,
                                                    year(devis.date) as dte
                                             FROM devis
                                             INNER JOIN client
                                             ON devis.client = client.id
                                             WHERE client.societe = {0}
                                             GROUP BY statut, dte
                                             ORDER BY dte DESC, statut;"
                                              , Id.ToString());
                DataTable Table = RecupererTable(sql);
                if (Table == null) return;

                Dictionary<int, ListeAvecTitre<Object>> pDic = new Dictionary<int, ListeAvecTitre<Object>>();
                foreach (DataRow Li in Table.Rows)
                {
                    int Index = (int)Convert.ChangeType(Li["statut"], typeof(int));

                    if (!pDicStatutDevis.ContainsKey(Index))
                        continue;

                    StatutDevis_e D = pDicStatutDevis[Index];

                    int Date = (int)Convert.ChangeType(Li["dte"], typeof(int));
                    ListeAvecTitre<Object> pListe = null;

                    if (pDic.ContainsKey(Date))
                        pListe = pDic[Date];
                    else
                    {
                        pListe = new ListeAvecTitre<Object>(Date.ToString());
                        pDic.Add(Date, pListe);
                    }

                    pListe.Add(new
                    {
                        Intitule = D.GetEnumDescription(),
                        Nb = AffNb(Li["nb"]),
                        Ht = AffNb(Li["ht"], "€"),
                        Marge = AffNb(Li["marge"], "€"),
                        Achat = AffNb(Li["achat"], "€"),
                        Deja_Facture = AffNb(Li["deja_facture"], "€"),
                        Reste_Facture = AffNb(Li["reste_facture"], "€")
                    });
                }

                ListeAnalyseDevis.Importer(pDic.Values);
            }

            #endregion

            #region FACTURE

            Dictionary<int, StatutFacture_e> pDicStatutFacture = new Dictionary<int, StatutFacture_e>();
            foreach (StatutFacture_e Statut in Enum.GetValues(typeof(StatutFacture_e)))
                pDicStatutFacture.Add((int)Statut, Statut);

            {
                String sql = String.Format(@"SELECT facture.statut,
                                                    count(facture.id) as nb,
                                                    sum(facture.prix_ht) as ht,
                                                    year(facture.date) as dte
                                             FROM ( facture
                                             INNER JOIN devis
                                             ON facture.devis = devis.id )
                                             INNER JOIN client
                                             ON devis.client = client.id
                                             WHERE client.societe = {0}
                                             GROUP BY facture.statut, dte
                                             ORDER BY dte DESC, facture.statut;"
                                              , Id.ToString());
                DataTable Table = RecupererTable(sql);
                if (Table == null) return;

                Dictionary<int, ListeAvecTitre<Object>> pDic = new Dictionary<int, ListeAvecTitre<Object>>();
                foreach (DataRow Li in Table.Rows)
                {
                    int Index = (int)Convert.ChangeType(Li["statut"], typeof(int));

                    if (!pDicStatutFacture.ContainsKey(Index))
                        continue;

                    StatutFacture_e F = pDicStatutFacture[Index];

                    int Date = (int)Convert.ChangeType(Li["dte"], typeof(int));
                    ListeAvecTitre<Object> pListe = null;

                    if (pDic.ContainsKey(Date))
                        pListe = pDic[Date];
                    else
                    {
                        pListe = new ListeAvecTitre<Object>(Date.ToString());
                        pDic.Add(Date, pListe);
                    }

                    pListe.Add(new
                    {
                        Intitule = F.GetEnumDescription(),
                        Nb = AffNb(Li["nb"]),
                        Ht = AffNb(Li["ht"], "€")
                    });
                }

                ListeAnalyseFacture.Importer(pDic.Values);
            }

            #endregion
        }

        #endregion
        // Dictionnaire des objets avec leurs propriétés et le nom du champ associé.
        // Structure :
        //              Dictionary<Type, Dictionary<String, PropertyInfo>>

        public static class DicProprietes
        {
            private static Dictionary<Type, Dictionary<String, PropertyInfo>> _DicPropriete = null;
            private static Dictionary<Type, PropertyInfo> _DicClePrimaire = null;
            private static Dictionary<Type, List<PropertyInfo>> _DicTri = null;

            static DicProprietes()
            {
                _DicPropriete = new Dictionary<Type, Dictionary<String, PropertyInfo>>();
                _DicClePrimaire = new Dictionary<Type, PropertyInfo>();
                _DicTri = new Dictionary<Type, List<PropertyInfo>>();

                List<Type> ListeTypes = Assembly.GetExecutingAssembly().GetTypes().ToList();
                
                int i = 0;
                while(i < ListeTypes.Count)
                {
                    Type T = ListeTypes[i];

                    if (T.BaseType != typeof(ObjetGestion))
                        ListeTypes.RemoveAt(i);
                    else
                        i++;
                }

                foreach (Type T in ListeTypes)
                {
                    List<PropertyInfo> pListeProp = T.GetProperties().Where(Prop => Attribute.IsDefined(Prop, typeof(Propriete), true)).ToList<PropertyInfo>();

                    Dictionary<String, PropertyInfo> pDicPropriete = new Dictionary<String, PropertyInfo>();
                    List<PropertyInfo> pListTri = new List<PropertyInfo>();

                    foreach (PropertyInfo Prop in pListeProp)
                    {
                        pDicPropriete.Add(NomChamp(Prop), Prop);

                        if (Attribute.IsDefined(Prop, typeof(ClePrimaire)) && !_DicClePrimaire.ContainsKey(T))
                            _DicClePrimaire.Add(T, Prop);

                        if (Attribute.IsDefined(Prop, typeof(Tri)))
                            pListTri.Add(Prop);
                    }

                    pListTri = pListTri.OrderBy(x => (x.GetCustomAttributes(typeof(Tri)).First() as Tri).No).ToList<PropertyInfo>();
                    _DicPropriete.Add(T, pDicPropriete);
                    _DicTri.Add(T, pListTri);
                }
            }

            public static List<Type> ListeType()
            {
                return _DicPropriete.Keys.ToList<Type>();
            }

            public static Dictionary<String, PropertyInfo> ListePropriete(Type T)
            {
                    return _DicPropriete[T];
            }

            public static PropertyInfo ClePrimaire(Type T)
            {
                return _DicClePrimaire[T];
            }

            public static List<PropertyInfo> ListeTri(Type T)
            {
                return _DicTri[T];
            }
        }

        // Dictionnaire des objets déjà crée.
        private static class DicObjet
        {
            private static Dictionary<Type, Dictionary<int, Object>> _DicBase = null;

            private static Dictionary<Type, Object> _DicListe = null;

            static DicObjet()
            {
                _DicBase = new Dictionary<Type,Dictionary<int,Object>>();
                _DicListe = new Dictionary<Type, Object>();
            }

            public static T RecupererObjet<T>(int id)
                where T : ObjetGestion
            {
                if (_DicBase.ContainsKey(typeof(T)))
                {
                    Dictionary<int, Object> pDic = _DicBase[typeof(T)];

                    if (pDic.ContainsKey(id))
                        return pDic[id] as T;
                }

                return null;
            }

            public static void ReferencerObjet(ObjetGestion Objet, Type T)
            {
                if (!Objet.EstSvgDansLaBase) return;

                Dictionary<int, Object> pDic;

                // Si le dictionnaire de base contient le Type de l'objet
                // on le récupère
                if (_DicBase.ContainsKey(T))
                {
                    pDic = _DicBase[T];
                }
                // Sinon, on le crée
                else
                {
                    pDic = new Dictionary<int, Object>();
                    _DicBase.Add(T, pDic);
                }

                // S'il ne contient pas l'objet, on l'ajoute
                if (!pDic.ContainsKey(Objet.Id))
                    pDic.Add(Objet.Id, Objet);
            }

            public static void SupprimerObjet(ObjetGestion Objet, Type T)
            {
                // Si le Type de l'objet existe, on récupère le dictionnaire
                if (_DicBase.ContainsKey(T))
                {
                    Dictionary<int, Object> pDic = _DicBase[T];

                    // Et s'il contient l'objet, on le supprime du dico
                    if (pDic.ContainsKey(Objet.Id))
                        pDic.Remove(Objet.Id);
                }
            }

            public static void ReferencerListe(Object Liste, Type T)
            {
                if(!_DicListe.ContainsKey(T))
                    _DicListe.Add(T, Liste);
            }

            public static ListeObservable<T> RecupererListe<T>()
                where T : ObjetGestion
            {
                if (_DicListe.ContainsKey(typeof(T)))
                    return _DicListe[typeof(T)] as ListeObservable<T>;

                return null;
            }
        }

        private class ListParametres : List<MySqlParameter>
        {
            public MySqlParameter Ajouter(MySqlParameter Param)
            {
                Add(Param);

                return Param;
            }

            public List<String> Noms
            {
                get
                {
                    List<String> pListe = new List<String>();
                    foreach (MySqlParameter item in this)
                        pListe.Add(item.ParameterName);

                    return pListe;
                }
            }

            public void Concatener(List<MySqlParameter> ListeBase)
            {
                foreach (MySqlParameter item in ListeBase)
                    this.Add(item);
            }
        }
    }
    
}

