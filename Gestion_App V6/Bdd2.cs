using LogDebugging;
using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace Gestion
{

    public static class Bdd2
    {
        private static Version VersionCourante = new Version(10);
        private static MySqlConnection _ConnexionBase = null;

        private static String FichierMapping = "MappingDesTables.xml";
        private static String FichierConnexion = "Connection.xml";

        public static String DB;
        public static String ConnexionCourante;

        //private static String _SvgNom = "SvgBase";
        //private static String _SvgExt = ".sql";

        static Bdd2() { }

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
            {
                Log.Message("La connexion a echoué");
                return false;
            }

            String pChemin = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @FichierMapping);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(pChemin);

            Version VersionBase = null;
            ListeObservable<Version> ListeVersion = null;

            try
            {
                var StructVersion = DicProp.Dic[typeof(Version)];
                if (TableExiste(StructVersion.NomTable))
                {
                    ListeVersion = Liste<Version>();

                    if ((ListeVersion != null) && (ListeVersion.Count > 0))
                        VersionBase = ListeVersion.OrderByDescending(i => i.No).First();
                }

                if (VersionBase != null)
                {
                    if (VersionCourante.No > VersionBase.No)
                    {
                        foreach (XmlNode Base in xmlDoc.SelectSingleNode("/Bases").ChildNodes)
                        {
                            int NoBase = (int)Convert.ChangeType(Base.Attributes["version"].Value, typeof(int));
                            if (ListeVersion.Count(x => x.No == NoBase) == 0)
                                MettreAJourLaBase(Base);
                        }

                        Bdd2.Ajouter<Version>(VersionCourante);
                        Bdd2.Enregistrer();
                    }
                }
                else
                {
                    foreach (XmlNode Base in xmlDoc.SelectSingleNode("/Bases").ChildNodes)
                        MettreAJourLaBase(Base);

                    Bdd2.Ajouter<Version>(VersionCourante);
                    Bdd2.Enregistrer();
                }
            }
            catch (Exception e)
            {
                Log.Message(e.ToString());
                Log.Message("Pd d'initialisation de la base");
                return false;
            }

            return true;
        }

        private static void MettreAJourLaBase(XmlNode Base)
        {
            try
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

                foreach (var StructObjet in DicProp.Dic.Values)
                {
                    Type Type = StructObjet.TypeObjet;
                    #region MODIFICATION DE LA STRUCTURE

                    CreerTable(Type);

                    String pNomNouvelleTable = StructObjet.NomTable;
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

                        foreach (Info info in DicProp.Dic[Type].ListeChampSql)
                        {
                            String pNomNewColonne = info.NomChampSql;
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
                                if (info.TypeChampSql.HasFlag(eTypeChampSql.ClePrimaire))
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

                            Log.Message(pSql);
                            Executer(pSql, null);
                        }

                        {
                            ListParametres pParams = new ListParametres();
                            pParams.Ajouter(new MySqlParameter("@AncienneTable", DbType.String)).Value = pNomAncienneTable;

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
                            if (Insert.Name == "Insert")
                            {

                                List<String> ListeColonne = new List<String>();
                                ListParametres pParams = new ListParametres();

                                foreach (XmlNode Valeur in Insert.ChildNodes)
                                    if (Valeur.Name == "Valeur")
                                    {
                                        ListeColonne.Add(Valeur.Attributes["name"].Value);
                                        pParams.Ajouter(new MySqlParameter("@" + Valeur.Attributes["name"].Value, Valeur.InnerText));
                                    }


                                String pSql = String.Format("INSERT INTO {0} ( {1} ) VALUES ( {2} );", pNomNouvelleTable, String.Join(" , ", ListeColonne), String.Join(" , ", pParams.Noms));

                                Executer(pSql, pParams);
                            }
                        }
                    }
                    #endregion
                }
            }
            catch (Exception e)
            {
                Log.Message(e.ToString());
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
            var StructObjet = DicProp.Dic[T];

            if (!TableExiste(StructObjet.NomTable))
            {
                List<String> pDicColonnes = new List<String>();

                foreach (var info in StructObjet.ListeChampSql)
                {
                    String Definition = "`" + info.NomChampSql + "` " + info.FormatChampSql + " " + info.ContrainteChampSql;

                    if (info.TypeChampSql.HasFlag(eTypeChampSql.ClePrimaire))
                        pDicColonnes.Insert(0, Definition);
                    else
                        pDicColonnes.Add(Definition);
                }

                pDicColonnes.Add("PRIMARY KEY (`" + StructObjet.ClePrimaire.NomChampSql + "`)");

                {
                    String Sql = String.Format("CREATE TABLE {0} ({1});", StructObjet.NomTable, String.Join(", ", pDicColonnes));

                    Executer(Sql, null);
                }

                return;
            }
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

        private static List<String> NomDesColonnes(String NomTable)
        {
            List<String> Liste = new List<string>();
            DataTable InfoTable = RecupererTable(String.Format("SELECT column_name FROM information_schema.columns WHERE table_name = '{0}'", NomTable));

            foreach (DataRow R in InfoTable.Rows)
                Liste.Add(R["column_name"].ToString());

            return Liste;
        }

        public static DateTime TempsRequete = new DateTime();

        public static DataTable RecupererTable(String sql)
        {
            DataTable dt = null;
            DbDataReader Lecteur = null;

            DateTime d = DateTime.Now;

            try
            {
                dt = new DataTable();
                MySqlCommand Cmde = new MySqlCommand(sql, _ConnexionBase);
                Lecteur = Cmde.ExecuteReader();
                dt.Load(Lecteur);
                Lecteur.Close();
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                if (Lecteur != null)
                    Lecteur.Close();
            }

            TempsRequete += DateTime.Now - d;

            Log.Message(TempsRequete + " => " + sql);

            return dt;
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

        private static String OLD()
        {
            return "old_";
        }
        private static String OLD(String NomTable)
        {
            return OLD() + NomTable;
        }

        // Renvoi l'id d'une ligne
        private static int IdLigne(DataRow Li, Type T)
        {
            return (int)Convert.ChangeType(Li[DicProp.Dic[T].ClePrimaire.NomChampSql], typeof(int));
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

        private static T ChargerObjet<T, U>(DataRow li, U parent)
            where T : ObjetGestion, new()
            where U : ObjetGestion
        {
            if (ModeChargerObjet) return null;

            ModeChargerObjet = true;

            // On récupère l'objet dans le dico
            T Objet = DicObjet.RecupererObjet<T>(IdLigne(li, typeof(T)));

            // S'il n'existe pas, on le crée et on le rempli
            if (Objet == null)
            {
                Objet = new T();

                // On récupère les propriétés
                var StructObjetT = DicProp.Dic[typeof(T)];

                // On charge la cle primaire en premier
                StructObjetT.ClePrimaire.Propriete.SetValue(Objet, Convert.ChangeType(li[StructObjetT.ClePrimaire.NomChampSql], StructObjetT.ClePrimaire.TypeObjet));

                // On charge la cle etrangere correpondant au parent
                // on charge l'objet s'il existe déjà dans la base, sinon
                // on renseigne l'ID de la cle etrangere
                foreach (var info in StructObjetT.ListeCleEtrangere)
                {
                    if ((parent != null) && (info.TypeObjet == typeof(U)))
                        info.Champ.SetValue(Objet, parent);
                    else
                    {
                        var objGestion = DicObjet.RecupererObjet((int)li[info.NomChampSql], info.TypeObjet);
                        if (objGestion == null)
                            info.ChampId.SetValue(Objet, (int)li[info.NomChampSql]);
                        else
                            info.Champ.SetValue(Objet, objGestion);
                    }
                }

                // Pour chacune d'elle, on les initialise correctement avec les valeurs de la base
                foreach (var info in StructObjetT.ListeInfo)
                {
                    if (li.Table.Columns.Contains(info.NomChampSql))
                    {
                        if (System.DBNull.Value == li[info.NomChampSql])
                            continue;

                        if (info.TypeObjet.IsEnum)
                            info.Champ.SetValue(Objet, System.Enum.Parse(info.TypeObjet, li[info.NomChampSql].ToString()));
                        else
                            info.Champ.SetValue(Objet, Convert.ChangeType(li[info.NomChampSql], info.TypeObjet));
                    }
                }

                DicObjet.ReferencerObjet(Objet, typeof(T));
            }

            Objet.EstCharge = true;

            ModeChargerObjet = false;

            return Objet;
        }

        public static T Parent<T, U>(U enfant)
            where T : ObjetGestion, new()
            where U : ObjetGestion
        {
            // Si on demande les enfants d'un Objet non sauvegardé dans la base, il y a un pb
            // on renvoi la liste vide.
            if ((enfant != null) && (!enfant.EstSvgDansLaBase))
            {
                Log.Methode("Bdd");
                Log.Write("Erreur ID = -1");
                Log.Write("Id : " + enfant.Id);
                return null;
            }

            var StructObjetT = DicProp.Dic[typeof(T)];
            var StructObjetU = DicProp.Dic[typeof(U)];

            foreach (var info in StructObjetU.ListeCleEtrangere)
            {
                if (info.TypeObjet == typeof(U) && info.ChampId.GetValue(enfant) != null)
                {
                    T Objet = DicObjet.RecupererObjet<T>((int)info.ChampId.GetValue(enfant));
                    info.ChampId.SetValue(enfant, null);
                    return Objet;
                }
            }

            String pQuery = String.Format("SELECT {0} FROM {1} WHERE {2} = {3};", StructObjetT.NomTable, StructObjetU.NomTable, StructObjetU.ClePrimaire.NomChampSql, enfant.Id);
            int pId = (int)Convert.ChangeType(RecupererChamp(pQuery, typeof(int)), typeof(int));

            pQuery = String.Format("SELECT * FROM {0} WHERE {1} = {2};", StructObjetT.NomTable, StructObjetT.ClePrimaire.NomChampSql, pId);
            DataTable Table = RecupererTable(pQuery);

            if ((Table == null) || (Table.Rows.Count == 0))
                return null;

            return ChargerObjet<T, T>(Table.Rows[0], null);
        }

        public static ListeObservable<T> Liste<T>()
            where T : ObjetGestion, new()
        {
            if (ModeChargerObjet || ModeAjouterObjet) return null;

            ListeObservable<T> pListe = DicObjet.RecupererListeObservable<T>();

            if (pListe == null)
            {
                pListe = Enfants<T, T>(null);
                DicObjet.ReferencerListeObservable(pListe, typeof(T));
            }

            return pListe;
        }

        public static ListeObservable<T> Enfants<T, U>(U parent)
            where T : ObjetGestion, new()
            where U : ObjetGestion
        {
            if (ModeChargerObjet || ModeAjouterObjet) return null;

            var StructObjetT = DicProp.Dic[typeof(T)];

            ListeObservable<T> pListe = new ListeObservable<T>();

            String pQuery = String.Format("SELECT * FROM {0}", StructObjetT.NomTable);

            // Si on demande les enfants d'un Objet non sauvegardé dans la base, il y a un pb
            // on renvoi la liste vide.
            if ((parent != null) && (!parent.EstSvgDansLaBase))
                return pListe;

            var StructObjetU = DicProp.Dic[typeof(U)];

            // Filtre sur un objet
            if (parent != null)
                pQuery += String.Format(" WHERE {0} = {1}", StructObjetU.NomTable, parent.Id);

            // S'il y a des clef de tri, on les ajoute
            List<String> pNomClesTri = StructObjetT.NomCleTri;
            if (pNomClesTri.Count > 0)
                pQuery += String.Format(" ORDER BY {0};", String.Join(", ", pNomClesTri));

            DataTable Table = RecupererTable(pQuery);

            // Si la requete est nulle, on renvoi une liste vide
            if (Table == null) return pListe;

            // Sinon, on charge les objets
            foreach (DataRow Li in Table.Rows)
            {
                T pObj = ChargerObjet<T, U>(Li, parent);
                pListe.Add(pObj);
            }

            return pListe;
        }

        public static void Ajouter<T>(T objet)
            where T : ObjetGestion
        {
            if (ModeChargerObjet) return;

            ModeAjouterObjet = true;

            var StructObjet = DicProp.Dic[typeof(T)];

            foreach (var info in StructObjet.ListeInfo)
            {
                String Defaut = DicIntitules.Defaut(typeof(T).Name, info.NomProp);

                if (info.TypeChampSql.HasFlag(eTypeChampSql.Max))
                {
                    int pVal = (int)RecupererChamp(String.Format("SELECT MAX({0}) FROM {1}", info.NomChampSql, StructObjet.NomTable), info.TypeObjet) + 1;
                    info.Champ.SetValue(objet, pVal);
                }
                else if (info.TypeObjet.IsEnum && !String.IsNullOrWhiteSpace(Defaut))
                    info.Champ.SetValue(objet, System.Enum.Parse(info.TypeObjet, Defaut));
                else if (!String.IsNullOrWhiteSpace(Defaut))
                    info.Champ.SetValue(objet, Convert.ChangeType(Defaut, info.TypeObjet));
                else if (info.Champ.GetValue(objet) == null)
                    info.Champ.SetValue(objet, info.TypeObjet.GetDefaultValue());
            }

            // Si on doit forcer l'ajout, on le rajoute à la ListeMaj
            // pour le mettre à jour avec les dernières valeurs
            if (StructObjet.ForcerAjout)
            {
                AjouterBase(objet, typeof(T));
                ListeMaj.Ajouter(objet, typeof(T));
            }
            else
                ListeAjouter.Ajouter(objet, typeof(T));

            objet.EstCharge = true;
            ModeAjouterObjet = false;
        }

        public static void Maj(ObjetGestion objet, Type T)
        {
            if (ModeChargerObjet || ModeAjouterObjet) return;

            ListeMaj.Ajouter(objet, T);
        }

        public static void Supprimer<T>(T objet)
            where T : ObjetGestion
        {
            if (objet.EstSvgDansLaBase)
            {
                ListeAjouter.Supprimer(objet);
                ListeMaj.Supprimer(objet);
                ListeSupprimer.Ajouter(objet, typeof(T));
            }

            objet = null;
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
                AjouterBase((ObjetGestion)v.Key, (Type)v.Value);

            foreach (DictionaryEntry v in ListeMaj)
                MajBase((ObjetGestion)v.Key, (Type)v.Value);

            foreach (DictionaryEntry v in ListeSupprimer)
                SupprimerBase((ObjetGestion)v.Key, (Type)v.Value);

            ListeAjouter.Clear();
            ListeMaj.Clear();
            ListeSupprimer.Clear();
        }

        private static void AjouterBase(ObjetGestion objet, Type T)
        {
            if (objet.EstSvgDansLaBase) return;

            var StructObjet = DicProp.Dic[T];

            List<String> ListeParam = new List<String>();
            ListParametres DicValeurs = new ListParametres();

            String PrefixClef = "@" + "Valeur_";

            // On initialise les clés etrangères avec -1
            foreach (var info in StructObjet.ListeCleEtrangere)
            {
                String Cle = PrefixClef + info.NomChampSql;

                ListeParam.Add(info.NomChampSql);

                var val = info.Champ.GetValue(objet);

                if (val != null)
                    DicValeurs.Ajouter(new MySqlParameter(Cle, MySqlDbType.Int32)).Value = Convert.ToInt32(val.ToString());
                else
                    DicValeurs.Ajouter(new MySqlParameter(Cle, MySqlDbType.Int32)).Value = -1;
            }

            foreach (var info in StructObjet.ListeInfo)
            {
                String Defaut = DicIntitules.Defaut(T.Name, info.NomProp);

                String Cle = PrefixClef + info.NomChampSql;

                ListeParam.Add(info.NomChampSql);

                if (info.TypeObjet.IsEnum)
                    DicValeurs.Ajouter(new MySqlParameter(Cle, MySqlDbType.Int32)).Value = Convert.ToInt32(info.Champ.GetValue(objet));

                else if (info.TypeObjet == typeof(Boolean))
                    DicValeurs.Ajouter(new MySqlParameter(Cle, MySqlDbType.Int32)).Value = Convert.ToInt32(info.Champ.GetValue(objet));

                else if (info.TypeObjet == typeof(String))
                    DicValeurs.Ajouter(new MySqlParameter(Cle, MySqlDbType.Text)).Value = info.Champ.GetValue(objet);

                else if (info.TypeObjet == typeof(DateTime))
                    DicValeurs.Ajouter(new MySqlParameter(Cle, MySqlDbType.Timestamp)).Value = info.Champ.GetValue(objet);

                else if (info.TypeObjet == typeof(Double))
                    DicValeurs.Ajouter(new MySqlParameter(Cle, MySqlDbType.Double)).Value = info.Champ.GetValue(objet);

                else if (info.TypeObjet == typeof(int))
                    DicValeurs.Ajouter(new MySqlParameter(Cle, MySqlDbType.Int32)).Value = info.Champ.GetValue(objet);

                else
                    DicValeurs.Ajouter(new MySqlParameter(Cle, Convert.ChangeType(Defaut, info.TypeObjet)));
            }

            {
                String pQuery = String.Format("INSERT INTO {0} ( {1} ) VALUES ( {2} );",
                    StructObjet.NomTable,
                    String.Join(" , ", ListeParam),
                    String.Join(" , ", DicValeurs.Noms)
                    );

                Log.Message(pQuery);

                ExecuterAsync(pQuery, DicValeurs);
            }

            objet.Id = DernierIdInsere(T);

            DicObjet.ReferencerObjet(objet, T);
        }

        private static void MajBase(ObjetGestion objet, Type T)
        {
            if (!objet.EstSvgDansLaBase) return;

            ListParametres ListeParam = new ListParametres();
            List<String> DicValeurs = new List<String>();

            var StructObjet = DicProp.Dic[T];
            String PrefixClef = "@" + "Valeur_";

            foreach (var info in StructObjet.ListeCleEtrangere)
            {
                var val = info.Propriete.GetValue(objet);

                if (val != null)
                {
                    String Cle = PrefixClef + info.NomChampSql;
                    DicValeurs.Add(info.NomChampSql + " = " + Cle);
                    ListeParam.Ajouter(new MySqlParameter(Cle, MySqlDbType.Int32)).Value = Convert.ToInt32(val.ToString());
                }
            }

            foreach (var info in StructObjet.ListeInfo)
            {
                var val = info.Propriete.GetValue(objet);

                if (val != null)
                {
                    String Cle = PrefixClef + info.NomChampSql;

                    DicValeurs.Add(info.NomChampSql + " = " + Cle);

                    if (info.TypeObjet.IsEnum)
                        ListeParam.Ajouter(new MySqlParameter(Cle, MySqlDbType.Int32)).Value = (int)val;
                    else if (info.TypeObjet == typeof(int))
                        ListeParam.Ajouter(new MySqlParameter(Cle, MySqlDbType.Int32)).Value = val;
                    else if (info.TypeObjet == typeof(Boolean))
                        ListeParam.Ajouter(new MySqlParameter(Cle, MySqlDbType.Int32)).Value = Convert.ToInt32(val);
                    else if (info.TypeObjet == typeof(Double))
                        ListeParam.Ajouter(new MySqlParameter(Cle, MySqlDbType.Double)).Value = Double.IsNaN((Double)val) ? 0 : (Double)val;
                    else if (info.TypeObjet == typeof(DateTime))
                        ListeParam.Ajouter(new MySqlParameter(Cle, MySqlDbType.Timestamp)).Value = val;
                    else if (info.TypeObjet == typeof(String))
                        ListeParam.Ajouter(new MySqlParameter(Cle, MySqlDbType.Text)).Value = val;
                    else
                        ListeParam.Ajouter(new MySqlParameter(Cle, val));
                }
            }

            ListeParam.Ajouter(new MySqlParameter(PrefixClef + StructObjet.ClePrimaire.NomChampSql, objet.Id));

            String pQuery = String.Format("UPDATE {0} SET {1} WHERE {2} = {3}", StructObjet.NomTable, String.Join(" , ", DicValeurs), StructObjet.ClePrimaire.NomChampSql, PrefixClef + StructObjet.ClePrimaire.NomChampSql);
            ExecuterAsync(pQuery, ListeParam);
        }

        private static void SupprimerBase(ObjetGestion objet, Type T)
        {
            if (!objet.EstSvgDansLaBase) return;

            DicObjet.SupprimerObjet(objet, T);

            String pQuery = String.Format("DELETE FROM {0} WHERE {1} = {2};", DicProp.Dic[T].NomTable, DicProp.Dic[T].ClePrimaire.NomChampSql, objet.Id.ToString());
            ExecuterAsync(pQuery, null);
        }

        public static Dictionary<Type, List<ObjetGestion>> PreCharger(Type T, List<ObjetGestion> listeObjet)
        {
            var StructObjetT = Bdd2.DicProp.Dic[T];

            var dicObjetsCharge = new Dictionary<Type, List<ObjetGestion>>();

            var DicIdEtObjet = new Dictionary<int, ObjetGestion>();
            foreach (var item in listeObjet)
            {
                item.EstPreCharge = true;
                DicIdEtObjet.Add(item.Id, item);
            }

            foreach (var liste in StructObjetT.ListeListeObjet)
            {
                var StructObjetE = Bdd2.DicProp.Dic[liste.TypeObjet];

                // On creer les listes pour qu'elles ne soit pas null
                for (int i = 0; i < listeObjet.Count; i++)
                {
                    var obj = listeObjet[i];

                    if (liste.Champ.GetValue(obj) == null)
                    {
                        var ListeParent = Activator.CreateInstance(liste.TypeListe);
                        TypedReference reference = __makeref(obj);
                        liste.Champ.SetValueDirect(reference, ListeParent);
                    }
                }

                String pQuery = String.Format("SELECT * FROM {0} WHERE {1} IN ({2})", StructObjetE.NomTable, StructObjetT.NomTable, String.Join(",", DicIdEtObjet.Keys));

                // S'il y a des clef de tri, on les ajoute
                List<String> pNomClesTri = StructObjetE.NomCleTri;
                if (pNomClesTri.Count > 0)
                    pQuery += String.Format(" ORDER BY {0};", String.Join(", ", pNomClesTri));

                DataTable Table = RecupererTable(pQuery);

                // Si la requete est nulle, on renvoi une liste vide
                if (Table == null) continue;

                var DicIdParentEtDataRow = new Dictionary<ObjetGestion, List<DataRow>>();

                // On tri les lignes par Id pour que cela soit plus rapide à charger
                foreach (DataRow li in Table.Rows)
                {
                    var id = (int)li[StructObjetT.NomTable];
                    var parent = DicIdEtObjet[id];
                    if (!DicIdParentEtDataRow.ContainsKey(parent))
                        DicIdParentEtDataRow.Add(parent, new List<DataRow>());

                    DicIdParentEtDataRow[parent].Add(li);
                }

                var listeObjetGestion = new List<ObjetGestion>();

                dicObjetsCharge.Add(StructObjetE.TypeObjet, listeObjetGestion);

                foreach (var item in DicIdParentEtDataRow)
                {
                    var parent = item.Key;
                    var ListeObjetsEnfants = liste.Champ.GetValue(parent);

                    foreach (var li in item.Value)
                    {
                        MethodInfo method_ChargerObjet = typeof(Bdd2).GetMethod("ChargerObjet", BindingFlags.NonPublic | BindingFlags.Static);
                        MethodInfo genericMethod_ChargerObjet = method_ChargerObjet.MakeGenericMethod(StructObjetE.TypeObjet, T);
                        var obj = (ObjetGestion)genericMethod_ChargerObjet.Invoke(null, new object[] { li, parent });

                        ListeObjetsEnfants.GetType().GetMethod("Add").Invoke(ListeObjetsEnfants, new[] { obj });

                        listeObjetGestion.Add(obj);
                    }


                }
            }

            return dicObjetsCharge;
        }

        // Dictionnaire des objets avec leurs propriétés et le nom du champ associé.
        // Structure :
        //              Dictionary<Type, Dictionary<String, PropertyInfo>>

        [Flags]
        public enum eTypeChampSql
        {
            Aucun = 0,
            ClePrimaire = 1,
            CleEtrangere = 2,
            Propriete = 4,
            ListeObjetGestion = 8,
            Tri = 16,
            Max = 32
        }

        public class Info
        {
            public Info(PropertyInfo propriete, FieldInfo champ, FieldInfo champId)
            {
                if (Attribute.IsDefined(propriete, typeof(Propriete), true))
                {
                    TypeChampSql = eTypeChampSql.Propriete;

                    if (Attribute.IsDefined(propriete, typeof(ClePrimaire)))
                        TypeChampSql |= eTypeChampSql.ClePrimaire;

                    else if (Attribute.IsDefined(propriete, typeof(CleEtrangere)))
                        TypeChampSql |= eTypeChampSql.CleEtrangere;

                    if (Attribute.IsDefined(propriete, typeof(Tri), false))
                    {
                        TypeChampSql |= eTypeChampSql.Tri;
                        Tri = propriete.GetCustomAttributes(typeof(Tri)).First() as Tri;
                    }

                    if (Attribute.IsDefined(propriete, typeof(Max), false))
                    {
                        TypeChampSql |= eTypeChampSql.Max;
                        Max = propriete.GetCustomAttributes(typeof(Max)).First() as Max;
                    }

                    var pAttProp = propriete.GetCustomAttributes(typeof(Propriete)).First() as Propriete;

                    if (pAttProp != null)
                    {
                        switch (pAttProp.Type)
                        {
                            case TypeSQL_e.cAuto:
                                if (propriete.PropertyType == typeof(int))
                                    FormatChampSql = "INTEGER";
                                else if (propriete.PropertyType == typeof(Boolean))
                                    FormatChampSql = "INTEGER";
                                else if (propriete.PropertyType == typeof(Double))
                                    FormatChampSql = "DOUBLE";
                                else if (propriete.PropertyType == typeof(DateTime))
                                    FormatChampSql = "TIMESTAMP";
                                else if (propriete.PropertyType == typeof(String))
                                    FormatChampSql = "TEXT";
                                else if (propriete.PropertyType.IsEnum)
                                    FormatChampSql = "INTEGER";
                                else
                                    FormatChampSql = "TEXT";
                                break;

                            case TypeSQL_e.cInt:
                                FormatChampSql = "INTEGER";
                                break;
                            case TypeSQL_e.cBool:
                                FormatChampSql = "INTEGER";
                                break;
                            case TypeSQL_e.cDbl:
                                FormatChampSql = "DOUBLE";
                                break;
                            case TypeSQL_e.cDate:
                                FormatChampSql = "TEXT";
                                break;
                            case TypeSQL_e.cString:
                                FormatChampSql = "TEXT";
                                break;
                            case TypeSQL_e.cEnum:
                                FormatChampSql = "INTEGER";
                                break;
                            case TypeSQL_e.cSerial:
                                FormatChampSql = "INTEGER";
                                break;
                            default:
                                FormatChampSql = "TEXT";
                                break;
                        }

                        ContrainteChampSql = pAttProp.Contrainte;
                    }
                }

                else if (Attribute.IsDefined(propriete, typeof(ListeObjetGestion)))
                    TypeChampSql = eTypeChampSql.ListeObjetGestion;

                if (TypeChampSql == eTypeChampSql.Aucun) return;

                Propriete = propriete;
                NomProp = propriete.Name;

                Champ = champ;
                NomChamp = champ.Name;

                NomChampSql = propriete.Name.ToLowerInvariant();

                if (champId != null)
                {
                    ChampId = champId;
                    NomChampId = champId.Name;
                }

                TypeObjet = propriete.PropertyType;

                if (TypeObjet.GetInterface(nameof(ICollection)) != null)
                {
                    TypeObjet = TypeObjet.GetGenericArguments()[0];
                    TypeListe = propriete.PropertyType;
                }
            }
            public Max Max { get; private set; } = null;
            public Tri Tri { get; private set; } = null;
            public String FormatChampSql { get; private set; } = "";
            public String ContrainteChampSql { get; private set; } = "";
            public eTypeChampSql TypeChampSql { get; private set; } = eTypeChampSql.Aucun;
            public Type TypeObjet { get; private set; }
            public Type TypeListe { get; private set; }
            public String NomProp { get; private set; }
            public PropertyInfo Propriete { get; private set; }
            public String NomChamp { get; private set; }
            public FieldInfo Champ { get; private set; }
            public String NomChampId { get; private set; }
            public FieldInfo ChampId { get; private set; }
            public String NomChampSql { get; private set; }
        }

        public class StructObjetGestion
        {
            public StructObjetGestion(Type t)
            {
                TypeObjet = t;

                // On recupère les champs dans un dictionnaire
                // avec comme clé le nom du champ moins la première lettre qui doit normalement
                // correspondre à un "_"
                var DicChamp = new Dictionary<String, FieldInfo>();
                foreach (var champ in TypeObjet.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                    DicChamp.Add(champ.Name.Substring(1), champ);

                foreach (var prop in TypeObjet.GetProperties())
                {
                    if (prop.Name == "EstPreCharge")
                        EstPreCharge = prop;

                    Info info = new Info(prop,
                                          DicChamp.ContainsKey(prop.Name) ? DicChamp[prop.Name] : null,
                                          DicChamp.ContainsKey("Id_" + prop.Name) ? DicChamp["Id_" + prop.Name] : null
                                          );

                    if (info.TypeChampSql.HasFlag(eTypeChampSql.Propriete))
                    {
                        ListeChampSql.Add(info);

                        if (info.TypeChampSql.HasFlag(eTypeChampSql.ClePrimaire))
                            ClePrimaire = info;

                        else if (info.TypeChampSql.HasFlag(eTypeChampSql.CleEtrangere))
                            ListeCleEtrangere.Add(info);

                        else
                            ListeInfo.Add(info);

                        if (info.TypeChampSql.HasFlag(eTypeChampSql.Tri))
                            ListeTri.Add(info);
                    }

                    else if (info.TypeChampSql.HasFlag(eTypeChampSql.ListeObjetGestion))
                        ListeListeObjet.Add(info);
                }

                // On tri les cles de 
                ListeTri = ListeTri.OrderBy(x => (x.Propriete.GetCustomAttributes(typeof(Tri)).First() as Tri).No).ToList();

                //===================================================================================================

                foreach (var info in ListeTri)
                {
                    var Tri = info.Propriete.GetCustomAttributes(typeof(Tri)).First() as Tri;
                    NomCleTri.Add(info.NomProp + " " + Tri.DirectionTriSql);
                }

                if (NomCleTri.Count == 0)
                    NomCleTri.Add(ClePrimaire.NomChampSql + " ASC");

                NomTable = TypeObjet.Name.ToString().ToLowerInvariant();

                if (Attribute.IsDefined(TypeObjet, typeof(ForcerAjout)))
                    ForcerAjout = true;
            }

            public Boolean ForcerAjout { get; private set; } = false;
            public String NomTable { get; private set; }
            public Type TypeObjet { get; private set; } = null;
            public PropertyInfo EstPreCharge { get; private set; } = null;
            public Info ClePrimaire { get; private set; } = null;
            public List<Info> ListeCleEtrangere { get; private set; } = new List<Info>();
            public List<Info> ListeInfo { get; private set; } = new List<Info>();
            public List<Info> ListeChampSql { get; private set; } = new List<Info>();
            public List<Info> ListeListeObjet { get; private set; } = new List<Info>();
            public List<Info> ListeTri { get; private set; } = new List<Info>();
            public List<String> NomCleTri { get; private set; } = new List<String>();

        }

        public static class DicProp
        {
            public static Dictionary<Type, StructObjetGestion> Dic { get; private set; }

            private static Boolean TypeEstObjetGestion(Type T)
            {
                Type U = T;

                if (T.IsAbstract) return false;

                while (U.BaseType != null)
                {
                    if (U.BaseType == typeof(ObjetGestion))
                        return true;

                    U = U.BaseType;
                }

                return false;
            }

            static DicProp()
            {
                Dic = new Dictionary<Type, StructObjetGestion>();

                List<Type> ListeTypes = Assembly.GetExecutingAssembly().GetTypes().Where(T => TypeEstObjetGestion(T)).ToList();

                foreach (Type T in ListeTypes)
                    Dic.Add(T, new StructObjetGestion(T));
            }

            public static List<Type> ListeType => Dic.Keys.ToList<Type>();
        }

        // Dictionnaire des objets déjà crée.
        private static class DicObjet
        {
            private static Dictionary<Type, Dictionary<int, ObjetGestion>> _DicBase = null;

            private static Dictionary<Type, Object> _DicListe = null;

            static DicObjet()
            {
                _DicBase = new Dictionary<Type, Dictionary<int, ObjetGestion>>();
                _DicListe = new Dictionary<Type, Object>();
            }

            public static T RecupererObjet<T>(int id)
                where T : ObjetGestion
            {
                if (_DicBase.ContainsKey(typeof(T)))
                {
                    Dictionary<int, ObjetGestion> pDic = _DicBase[typeof(T)];

                    if (pDic.ContainsKey(id))
                        return pDic[id] as T;
                }

                return null;
            }

            public static List<T> RecupererListeObjet<T>()
                where T : ObjetGestion
            {
                List<T> Liste = new List<T>();

                foreach (var val in _DicBase[typeof(T)].Values)
                    Liste.Add((T)val);

                return Liste;
            }

            public static List<ObjetGestion> RecupererListeObjetGestion(Type T)
            {
                return _DicBase[T].Values.ToList();
            }

            public static ObjetGestion RecupererObjet(int id, Type T)
            {
                if (_DicBase.ContainsKey(T))
                {
                    var pDic = _DicBase[T];

                    if (pDic.ContainsKey(id))
                        return pDic[id];
                }

                return null;
            }

            public static void ReferencerObjet(ObjetGestion Objet, Type T)
            {
                if (!Objet.EstSvgDansLaBase) return;

                Dictionary<int, ObjetGestion> pDic;

                // Si le dictionnaire de base contient le Type de l'objet
                // on le récupère
                if (_DicBase.ContainsKey(T))
                {
                    pDic = _DicBase[T];
                }
                // Sinon, on le crée
                else
                {
                    pDic = new Dictionary<int, ObjetGestion>();
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
                    var pDic = _DicBase[T];

                    // Et s'il contient l'objet, on le supprime du dico
                    if (pDic.ContainsKey(Objet.Id))
                        pDic.Remove(Objet.Id);
                }
            }

            public static void ReferencerListeObservable(Object Liste, Type T)
            {
                if (!_DicListe.ContainsKey(T))
                    _DicListe.Add(T, Liste);
            }

            public static ListeObservable<T> RecupererListeObservable<T>()
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

    public static class GenererAnalyse
    {
        private static String AffNb(Object Obj, String Unite = "")
        {
            return (Math.Round(((Double)Convert.ChangeType(Obj, typeof(Double)))).ToString() + " " + Unite).Trim();
        }

        public static void Client(ref ListeAvecTitre<ListeAvecTitre<Object>> ListeAnalyseDevis, ref ListeAvecTitre<ListeAvecTitre<Object>> ListeAnalyseFacture, int Id)
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
                DataTable Table = Bdd2.RecupererTable(sql);
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
                DataTable Table = Bdd2.RecupererTable(sql);
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

        public static void Societe(ref ListeAvecTitre<ListeAvecTitre<Object>> ListeAnalyseDevis, ref ListeAvecTitre<ListeAvecTitre<Object>> ListeAnalyseFacture, int Id)
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
                DataTable Table = Bdd2.RecupererTable(sql);
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
                DataTable Table = Bdd2.RecupererTable(sql);
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
    }

}

