using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Gestion
{
    public enum StatutDevis_e
    {
        [Description("En cours")]
        cEnCours = 1,
        [Description("Envoyé")]
        cEnvoye = 2,
        [Description("Validé")]
        cValide = 3,
        [Description("Terminé")]
        cTermine = 4,
        [Description("Refusé")]
        cRefuse = 5,
        [Description("Indicé")]
        cIndice = 6
    }

    [ForcerAjout]
    public partial class Devis : ObjetGestion
    {
        public Devis() { }

        public Devis(Client C)
        {
            Client = C;
            Bdd1.Ajouter(this);

            // On rajoute le prefix après pour être sûr qu'il ne sera pas ecrasé par une valeur par defaut
            Prefix_Utilisateur = Client.Societe.PrefixUtilisateurCourant;
        }

        private String RefBase { get { return "D" + No; } }

        private String RefStd { get { return Prefix_Utilisateur + "D" + No; } }

        private String RefStdAvecIndice { get { return Prefix_Utilisateur + RefBase + "-" + Indice; } }

        public String NomDossierDevis
        {
            get
            {
                String pDescription = Description.Flat().PrepareForFilename();

                if (!String.IsNullOrWhiteSpace(pDescription))
                {
                    pDescription = " - " + pDescription;

                    if (pDescription.Length > 99)
                        pDescription = pDescription.Substring(0, 100);
                }

                return (RefStd + pDescription).Trim();
            }
        }

        public String NomDossierIndice
        {
            get
            {
                return Indice.ToString();
            }
        }

        private void CreerStructureDossier()
        {
            DirectoryInfo pDossierIndice = DossierIndice;
            if (pDossierIndice == null) return;

            String Chemin = Path.Combine(pDossierIndice.FullName, Properties.Settings.Default.NomFichierInfos);
            if (File.Exists(Chemin))
                File.Delete(Chemin);

            using (StreamWriter sw = new StreamWriter(Chemin, false, Encoding.GetEncoding(1252)))
            {
                sw.WriteLine("Client : " + Client.Intitule.Flat().Trim());
                sw.WriteLine("Adresse : " + (Adresse_Client.Adresse + " - " + Adresse_Client.Cp + " " + Adresse_Client.Ville).Trim());
                sw.WriteLine("NoCommande : " + Ref);
                sw.WriteLine("NoClient : " + Client.Ref);
                sw.WriteLine("Dessinateur : " + Client.Societe.UtilisateurCourant.Intitule);
            }
        }

        public DirectoryInfo CreerDossier(Boolean Forcer = false)
        {
            try
            {
                if (Client.Dossier == null)
                    Client.CreerDossier();

                DirectoryInfo pDossierClient = Client.Dossier;
                DirectoryInfo pDossier = Dossier;
                if ((pDossierClient != null) && (pDossier == null))
                {
                    // Dossier de base
                    String Chemin = Path.Combine(pDossierClient.FullName, NomDossierDevis);
                    DirectoryInfo pDossierDevis = Directory.CreateDirectory(Chemin);

                    // Dossier de l'indice
                    Chemin = Path.Combine(pDossierDevis.FullName, NomDossierIndice);
                    DirectoryInfo pDossierIndice = Directory.CreateDirectory(Chemin);
                    CreerStructureDossier();
                    return pDossierIndice;
                }
                
                if (Forcer && (pDossier != null))
                {
                    // Dossier de base
                    String Chemin = Path.Combine(pDossierClient.FullName, NomDossierDevis);

                    if (pDossier.FullName != Chemin)
                        pDossier.MoveTo(Chemin);


                    DirectoryInfo pDossierIndice = DossierIndice;
                    if (pDossierIndice == null)
                    {
                        // Dossier de l'indice
                        Chemin = Path.Combine(Dossier.FullName, NomDossierIndice);
                        pDossierIndice = Directory.CreateDirectory(Chemin);
                    }

                    CreerStructureDossier();
                    return pDossierIndice;
                }
            }
            catch { }

            return null;
        }

        public DirectoryInfo Dossier
        {
            get
            {
                DirectoryInfo pDossierClient = Client.Dossier;
                if (pDossierClient != null)
                {
                    String Chemin = Path.Combine(pDossierClient.FullName, NomDossierDevis);

                    if (Directory.Exists(Chemin))
                        return new DirectoryInfo(Chemin);

                    var CheminDossierClient = pDossierClient.FullName;

                    var refBase = RefBase;
                    String cheminDossier = "";

                    Parallel.ForEach(Directory.EnumerateDirectories(CheminDossierClient),
                        (CheminDossier, Etat) =>
                        {
                            var Result = Path.GetFileName(CheminDossier).Split(new char[] { '-' }, 2);
                            if (Result.Length == 0) return;

                            if(Result[0].Trim().EndsWith(refBase))
                            {
                                cheminDossier = CheminDossier;
                                Etat.Stop();
                                return;
                            }
                        }
                    );

                    if(!String.IsNullOrWhiteSpace(cheminDossier) && Directory.Exists(cheminDossier))
                        return new DirectoryInfo(cheminDossier);
                }

                return null;
            }
        }

        public DirectoryInfo DossierIndice
        {
            get
            {
                DirectoryInfo pDossierDevis = Dossier;
                if (pDossierDevis != null)
                {
                    String Chemin = Path.Combine(pDossierDevis.FullName, NomDossierIndice);

                    if (Directory.Exists(Chemin))
                        return new DirectoryInfo(Chemin);

                    String[] ListeDossier = Directory.GetDirectories(pDossierDevis.FullName, NomDossierIndice + " *", SearchOption.TopDirectoryOnly);

                    if (ListeDossier.GetLength(0) > 0)
                        return new DirectoryInfo(ListeDossier[0]);
                }

                return null;
            }
        }

        protected override void MajRef(String reference = null)
        {
            base.MajRef(RefStdAvecIndice);
        }

        [Propriete, Max, NePasCopier]
        [Tri(No = 2, DirectionTri = ListSortDirection.Descending)]
        public override int No
        {
            get { return base.No; }
            set { base.No = value; }
        }

        private int _Indice = 0;
        [Propriete, NePasCopier]
        [Tri(No = 3, DirectionTri = ListSortDirection.Descending)]
        public int Indice
        {
            get { return _Indice; }
            set { Set(ref _Indice, value, this); }
        }

        private Client _Client = null;
        [CleEtrangere]
        public Client Client
        {
            get
            {
                if (_Client == null)
                    _Client = Bdd1.Parent<Client, Devis>(this);

                return _Client;
            }
            set
            {
                Client OldClient = _Client;


                Set(ref _Client, value, this);

                if (_Client.ListeDevis != null)
                    _Client.ListeDevis.Add(this);

                // Si un client est déjà lié
                if (OldClient != null)
                    OldClient.ListeDevis.Remove(this);
            }
        }

        private Adresse_Client _Adresse_Client = null;
        [CleEtrangere]
        public Adresse_Client Adresse_Client
        {
            get
            {
                if (_Adresse_Client == null)
                    _Adresse_Client = Bdd1.Parent<Adresse_Client, Devis>(this);


                if (_Adresse_Client == null)
                {
                    ListeObservable<Adresse_Client> Liste = Client.ListeAdresse_Client;
                    if (Liste != null)
                        _Adresse_Client = Liste[0];
                }

                return _Adresse_Client;
            }
            set { Set(ref _Adresse_Client, value, this); }
        }

        private StatutDevis_e _Statut = StatutDevis_e.cEnCours;
        [Propriete, NePasCopier]
        public StatutDevis_e Statut
        {
            get { return _Statut; }
            set { Set(ref _Statut, value, this); }
        }

        private DateTime _Date = DateTime.Now;
        [Propriete, NePasCopier]
        public DateTime Date
        {
            get { return _Date; }
            set { Set(ref _Date, value, this); }
        }

        private String _Conditions = "Acompte de 30 % à la commande\r\n50% à la pose\r\nSolde à la récéption de l'ouvrage\r\nDevis valable deux mois";
        [Propriete]
        public String Conditions
        {
            get { return _Conditions; }
            set { Set(ref _Conditions, value, this); }
        }

        private String _Description = "";
        [Propriete]
        public String Description
        {
            get { return _Description; }
            set { Set(ref _Description, value, this); }
        }

        private String _Commentaires = "";
        [Propriete]
        public String Commentaires
        {
            get { return _Commentaires; }
            set { Set(ref _Commentaires, value, this); }
        }

        private String _Info = "";
        [Propriete]
        public String Info
        {
            get { return _Info; }
            set { Set(ref _Info, value, this); }
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

        private Double _Marge_Pct = 0;
        [Propriete]
        public Double Marge_Pct
        {
            get { return _Marge_Pct; }
            set { Set(ref _Marge_Pct, value, this); }
        }

        private Double _Tva_Pct = 0;
        [Propriete]
        public Double Tva_Pct
        {
            get { return _Tva_Pct; }
            set
            {
                Set(ref _Tva_Pct, value, this);
                CalculerTva();
            }
        }

        private Double _Tva = 0;
        [Propriete]
        public Double Tva
        {
            get { return _Tva; }
            set
            {
                Set(ref _Tva, value, this);
                CalculerTtc();
            }
        }

        private Double _Prix_Ttc = 0;
        [Propriete]
        public Double Prix_Ttc
        {
            get { return _Prix_Ttc; }
            set { Set(ref _Prix_Ttc, value, this); }
        }

        private int _Acompte_Pct = 0;
        [Propriete]
        public int Acompte_Pct
        {
            get { return _Acompte_Pct; }
            set
            {
                Set(ref _Acompte_Pct, value, this);
                Acompte = Math.Round(Prix_Ttc * value / 100.0);
            }
        }

        private Double _Acompte = 0;
        [Propriete]
        public Double Acompte
        {
            get { return _Acompte; }
            set { Set(ref _Acompte, value, this); }
        }

        private Double _Deja_Facture_Ht = 0;
        [Propriete]
        public Double Deja_Facture_Ht
        {
            get { return _Deja_Facture_Ht; }
            set { Set(ref _Deja_Facture_Ht, value, this); }
        }

        private Double _Reste_A_Facture_Ht = 0;
        [Propriete]
        public Double Reste_A_Facture_Ht
        {
            get { return _Reste_A_Facture_Ht; }
            set { Set(ref _Reste_A_Facture_Ht, value, this); }
        }

        private Double _Prix_Tt_Achat = 0;
        [Propriete]
        public Double Prix_Tt_Achat
        {
            get { return _Prix_Tt_Achat; }
            set { Set(ref _Prix_Tt_Achat, value, this); }
        }

        private Double _Prix_Tt_Heure = 0;
        [Propriete]
        public Double Prix_Tt_Heure
        {
            get { return _Prix_Tt_Heure; }
            set { Set(ref _Prix_Tt_Heure, value, this); }
        }

        private Double _Nb_Heure = 0;
        [Propriete]
        public Double Nb_Heure
        {
            get { return _Nb_Heure; }
            set { Set(ref _Nb_Heure, value, this); }
        }

        private Boolean _Favori = false;
        [Propriete]
        [Tri(No = 1, DirectionTri = ListSortDirection.Descending)]
        public Boolean Favori
        {
            get { return _Favori; }
            set { Set(ref _Favori, value, this); }
        }

        private BitmapImage _Apercu = null;

        public BitmapImage Apercu
        {
            get
            {
                // Si l'utilisateur le demande, on met à jour les vignettes en temps réel
                if (Client.Societe.UtilisateurCourant.MajVignette) _Apercu = null;

                if (_Apercu == null)
                {
                    DirectoryInfo D = DossierIndice;

                    if (D == null)
                        D = Dossier;

                    if (D == null) return null;

                    List<String> Filtres = new List<String>() { @"Apercu\.(jpg|png|bmp)", @"^(?!~)(.*)\.(sldasm|sldprt)", @"^(?!~)(.*)\.(slddrw)" };
                    List<String> Fichiers = new List<String>(Directory.GetFiles(D.FullName));
                    Fichiers.Sort(new WindowsStringComparer());

                    foreach (String Filtre in Filtres)
                    {
                        String F = Fichiers.Find(n => { return Regex.IsMatch(Path.GetFileName(n), Filtre, RegexOptions.IgnoreCase); });
                        if (F != default(String))
                        {
                            Bitmap thumbnail = WindowsThumbnailProvider.GetThumbnail(F, 256, 256, ThumbnailOptions.None);
                            BitmapImage img = WindowsThumbnailProvider.ToBitmapImage(WindowsThumbnailProvider.AutoCrop(thumbnail));

                            thumbnail.Dispose();

                            _Apercu = img;
                            break;
                        }
                    }
                }

                return _Apercu;
            }
        }

        private ListeObservable<Poste> _ListePoste = null;
        public ListeObservable<Poste> ListePoste
        {
            get
            {
                if (_ListePoste == null)
                {
                    _ListePoste = Bdd1.Enfants<Poste, Devis>(this);
                    if ((_ListePoste != null) && (_ListePoste.Count > 0) && (_ListePoste[0].No == 0))
                        _ListePoste.Numeroter();
                }

                return _ListePoste;
            }
        }

        private ListeObservable<Facture> _ListeFacture = null;
        public ListeObservable<Facture> ListeFacture
        {
            get
            {
                if (_ListeFacture == null)
                    _ListeFacture = Bdd1.Enfants<Facture, Devis>(this);

                return _ListeFacture;
            }
        }

        private ListeObservable<Achat> _ListeAchat = null;
        public ListeObservable<Achat> ListeAchat
        {
            get
            {
                if (_ListeAchat == null)
                    _ListeAchat = Bdd1.Enfants<Achat, Devis>(this);

                return _ListeAchat;
            }
        }

        private ListeObservable<Heure> _ListeHeure = null;
        public ListeObservable<Heure> ListeHeure
        {
            get
            {
                if (_ListeHeure == null)
                    _ListeHeure = Bdd1.Enfants<Heure, Devis>(this);

                return _ListeHeure;
            }
        }

        private ListeAvecTitre<Object> _ListeAnalyseCode = null;
        public ListeAvecTitre<Object> ListeAnalyseCode
        {
            get
            {
                if (_ListeAnalyseCode == null)
                    Analyser();

                return _ListeAnalyseCode;
            }
        }

        private ListeAvecTitre<Object> _ListeAnalyseFamille = null;
        public ListeAvecTitre<Object> ListeAnalyseFamille
        {
            get
            {
                if (_ListeAnalyseFamille == null)
                    Analyser();

                return _ListeAnalyseFamille;
            }
        }

        private class Pair<T>
        {
            public T Qte { get; set; }
            public T Tt { get; set; }

            public Pair(T qte, T tt)
            {
                Qte = qte;
                Tt = tt;
            }
        }

        public void Analyser()
        {
            if (!EstCharge) return;

            Func<Object, String> AffNb = delegate (Object Obj)
            {
                return Math.Round(((Double)Convert.ChangeType(Obj, typeof(Double)))).ToString();
            };

            Func<Object, String, String> AffNbU = delegate (Object Obj, String Unite)
            {
                return (Math.Round(((Double)Convert.ChangeType(Obj, typeof(Double)))).ToString() + " " + Unite).Trim();
            };

            if (_ListeAnalyseCode == null)
                _ListeAnalyseCode = new ListeAvecTitre<Object>("Code");
            else
                _ListeAnalyseCode.Clear();

            if (_ListeAnalyseFamille == null)
                _ListeAnalyseFamille = new ListeAvecTitre<Object>("Famille");
            else
                _ListeAnalyseFamille.Clear();

            Dictionary<CodeFamille_e, Pair<Double>> pDicCode = new Dictionary<CodeFamille_e, Pair<Double>>();
            foreach (CodeFamille_e Code in Enum.GetValues(typeof(CodeFamille_e)))
            {
                if (!String.IsNullOrWhiteSpace(Code.GetEnumDescription()))
                    pDicCode.Add(Code, new Pair<Double>(0, 0));
            }

            Dictionary<Famille, Pair<Double>> pDicFamille = new Dictionary<Famille, Pair<Double>>();
            foreach (Famille Famille in Client.Societe.ListeFamille)
            {
                if (pDicCode.ContainsKey(Famille.Code))
                    pDicFamille.Add(Famille, new Pair<Double>(0, 0));
            }

            foreach (Poste P in ListePoste)
            {
                foreach (Ligne_Poste L in P.ListeLignePoste)
                {
                    Double Qte = L.Qte * P.Qte;
                    if (L.Prix_Forfaitaire)
                        Qte = L.Qte;

                    Double Tt = L.Debours_Unitaire * P.Qte;

                    if (pDicCode.ContainsKey(L.Famille.Code))
                    {
                        Pair<Double> T = pDicCode[L.Famille.Code];
                        T.Qte += Qte; T.Tt += Tt;
                    }

                    if (pDicFamille.ContainsKey(L.Famille))
                    {
                        Pair<Double> T = pDicFamille[L.Famille];
                        T.Qte += Qte; T.Tt += Tt;
                    }
                }
            }

            foreach (KeyValuePair<CodeFamille_e, Pair<Double>> Kv in pDicCode)
            {
                if (Kv.Value.Qte != 0)
                    _ListeAnalyseCode.Add(new
                    {
                        Intitule = Kv.Key.GetEnumDescription(),
                        Qte = AffNbU(Kv.Value.Qte, Kv.Key.GetEnumUnite()),
                        Tt = AffNbU(Kv.Value.Tt, "€")
                    });
            }

            foreach (KeyValuePair<Famille, Pair<Double>> Kv in pDicFamille)
            {
                if (Kv.Value.Qte != 0)
                    _ListeAnalyseFamille.Add(new
                    {
                        Intitule = Kv.Key.Description,
                        Qte = AffNbU(Kv.Value.Qte, Kv.Key.Unite),
                        Tt = AffNbU(Kv.Value.Tt, "€")
                    });
            }
        }

        private void CalculerTva()
        {
            if (!EstCharge) return;

            Tva = ArrondiEuro(Prix_Ht * Tva_Pct / 100);

            foreach (Facture Facture in ListeFacture)
            {
                Facture.Calculer();
            }
        }

        private void CalculerTtc()
        {
            if (!EstCharge) return;

            Prix_Ttc = ArrondiEuro(Prix_Ht + Tva);
        }

        public void Calculer()
        {
            if (!EstCharge) return;

            Double pPrix_Ht = 0;
            Double pMarge = 0;
            foreach (Poste Poste in ListePoste)
            {
                if (!Poste.Statut)
                    continue;

                pPrix_Ht += Poste.Prix_Ht;
                pMarge += Poste.Marge;
            }

            Prix_Ht = pPrix_Ht;
            Marge = pMarge;
            //Marge_Pct = ArrondiPct((Marge / Prix_Ht) * 100.0);
            Marge_Pct = ArrondiPct(((Prix_Ht / (Prix_Ht - Marge)) - 1) * 100);

            CalculerTva();
            CalculerTtc();

            Acompte = Math.Round(Prix_Ht * Acompte_Pct / 100.0) * (1 + Tva_Pct / 100.0);

            CalculerFacture();

            Analyser();
        }

        public void CalculerFacture()
        {
            Double pDeja_Facture_Ht = 0;
            foreach (Facture Facture in ListeFacture)
            {
                pDeja_Facture_Ht += Facture.Prix_Ht;
            }

            Deja_Facture_Ht = pDeja_Facture_Ht;
            Reste_A_Facture_Ht = Math.Max(Prix_Ht - Deja_Facture_Ht, 0);
        }

        public void CalculerAchat()
        {
            Double pTT = 0;
            foreach (Achat A in ListeAchat)
            {
                pTT += A.Prix;
            }

            Prix_Tt_Achat = pTT;
        }

        public override Boolean Supprimer()
        {
            if (!EstCharge) return false;

            foreach (Facture F in _ListeFacture)
                Client.ListeFacture.Remove(F);

            SupprimerListe(_ListePoste);
            SupprimerListe(_ListeFacture);
            SupprimerListe(_ListeAchat);
            SupprimerListe(_ListeHeure);

            try
            {
                // Suppression des dossiers s'il sont vide
                DirectoryInfo pDossierIndice = DossierIndice;
                if (pDossierIndice != null)
                {
                    List<DirectoryInfo> ListeDossierIndice = new List<DirectoryInfo>(pDossierIndice.GetDirectories());
                    List<FileInfo> ListeFichier = new List<FileInfo>(pDossierIndice.GetFiles());

                    if ((ListeDossierIndice.Count == 0) && (ListeFichier.Count == 1) && (ListeFichier[0].Name == Properties.Settings.Default.NomFichierInfos))
                        Directory.Delete(pDossierIndice.FullName, true);

                    DirectoryInfo pDossier = Dossier;
                    if (pDossier != null)
                    {
                        List<DirectoryInfo> ListeDossier = new List<DirectoryInfo>(pDossier.GetDirectories());
                        ListeFichier = new List<FileInfo>(pDossier.GetFiles());
                        if ((ListeDossier.Count == 0) && (ListeFichier.Count == 0))
                            Directory.Delete(pDossier.FullName);
                    }
                }
            }
            catch { }

            if (Client != null)
                Client.ListeDevis.Remove(this);

            Bdd1.Supprimer(this);

            return true;
        }

        public override void Copier<T>(T ObjetBase)
        {
            Devis DevisBase = ObjetBase as Devis;
            if ((!EstCharge) || (DevisBase == null) || (!DevisBase.EstCharge)) return;

            CopierBase(DevisBase);

            Importer(DevisBase);
        }

        public void Importer(Devis DevisBase)
        {
            if ((!EstCharge) || (DevisBase == null) || (!DevisBase.EstCharge)) return;

            foreach (Poste Poste in DevisBase.ListePoste)
            {
                Poste pNewPoste = new Poste(this);
                pNewPoste.Copier(Poste);
            }
        }

        public void CopierAvecIndice(Devis ObjetBase)
        {
            if (!EstCharge) return;

            Copier(ObjetBase);

            // D'abord copier l'indice
            Indice = ObjetBase.Client.ListeDevis.Where(x => x.No == ObjetBase.No).Max(y => y.Indice) + 1;
            // Puis le numero pour eviter le cas ou l'indice de this serait déjà supérieur
            No = ObjetBase.No;

        }
    }
}