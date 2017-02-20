using LogDebugging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Gestion
{
    public class ObjetGestion : INotifyPropertyChanged
    {

        public static readonly int DEFAULT_ID = -1;
        protected static readonly int DEFAULT_ARRONDI_EURO = 2;
        protected static readonly int DEFAULT_ARRONDI_PCT = 1;

        protected static Double ArrondiEuro(Double Val)
        {
            return Math.Round(Val, DEFAULT_ARRONDI_EURO);
        }

        protected static Double ArrondiPct(Double Val)
        {
            return Math.Round(Val, DEFAULT_ARRONDI_PCT);
        }

        protected Type T = null;

        protected int _Id = DEFAULT_ID;
        private Boolean _EstCharge = false;

        public Boolean EstSvgDansLaBase
        {
            get { return _Id != DEFAULT_ID; }
        }

        [ClePrimaire]
        public int Id
        {
            get { return _Id; }
            set { _Id = value; }
        }

        public Boolean EstCharge
        { get { return _EstCharge; } set { _EstCharge = value; } }

        protected String _Ref = "";
        [Propriete, NePasCopier]
        public virtual String Ref
        {
            get { return _Ref; }
            set { Set(ref _Ref, value, this); }
        }

        protected String _Prefix_Utilisateur = "";
        [Propriete, NePasCopier]
        public virtual String Prefix_Utilisateur
        {
            get { return _Prefix_Utilisateur; }
            set { Set(ref _Prefix_Utilisateur, value, this); }
        }

        protected int _No = 0;
        [Propriete, Max, Tri]
        public virtual int No
        {
            get { return _No; }
            set { Set(ref _No, value, this); }
        }

        public ObjetGestion()
        {
            T = this.GetType();
        }

        protected virtual void MajRef(String reference = null)
        {
            if(String.IsNullOrWhiteSpace(reference))
                reference = Prefix_Utilisateur + No.ToString();

            if (reference != _Ref)
                Ref = reference;
        }

        public virtual Boolean Supprimer()
        { return false; }

        protected Boolean CopierBase<T>(T ObjetBase)
            where T : ObjetGestion
        {
            if (!ObjetBase.EstCharge || !this.EstCharge) return false;

            try
            {
                List<PropertyInfo> pListeProp = typeof(T).GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(Propriete), true)).ToList<PropertyInfo>();

                foreach (PropertyInfo Prop in pListeProp)
                {
                    if (Attribute.IsDefined(Prop, typeof(ForcerCopie)) || !(Attribute.IsDefined(Prop, typeof(ClePrimaire)) || Attribute.IsDefined(Prop, typeof(CleEtrangere)) || Attribute.IsDefined(Prop, typeof(NePasCopier))))
                        Prop.SetValue(this, Prop.GetValue(ObjetBase));
                }
            }
            catch(Exception e)
            {
                this.LogMethode(e);
                return false;
            }

            return true;
        }

        public virtual void Copier<T>(T ObjetBase)
            where T : ObjetGestion
        {
            CopierBase(ObjetBase);
        }

        protected void SupprimerListe<T>(ListeObservable<T> Liste)
            where T : ObjetGestion
        {
            if (Liste != null)
                while (Liste.Count > 0)
                    Liste[0].Supprimer();
        }

        public override string ToString()
        {
            return _Id.ToString();
        }

        #region Notification WPF

        protected bool Set<U, V>(ref U field, U value, V Objet, [CallerMemberName]string propertyName = "")
            where V : ObjetGestion
        {
            if (EqualityComparer<U>.Default.Equals(field, value)) { return false; }
            field = value;
            MajRef();
            OnPropertyChanged(propertyName);
            if (EstSvgDansLaBase)
                Bdd.Maj(Objet, T, propertyName);
            return true;
        }

        protected bool Set<U>(ref U field, U value, [CallerMemberName]string propertyName = "")
        {
            if (EqualityComparer<U>.Default.Equals(field, value)) { return false; }
            field = value;
            MajRef();
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] String NomProp = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(NomProp));
            }
        }

        #endregion
    }

    public class ListeObservable<T> : ObservableCollection<T>
    {
        private String _Intitule = null;
        private Dictionary<String, String> _ListeEntete = null;

        public String Intitule
        {
            get
            {
                if (_Intitule == null)
                    _Intitule = DicIntitules.IntituleType(typeof(T).Name);

                return _Intitule;
            }
        }
        public Dictionary<String, String> ListeEntete
        {
            get
            {
                if (_ListeEntete == null)
                    _ListeEntete = DicIntitules.DicEntete(typeof(T).Name);

                return _ListeEntete;
            }
        }

        public void Numeroter()
        {
            // On liste les proprietes triables
            List<PropertyInfo> pListeProp = typeof(T).GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(Tri), true)).ToList<PropertyInfo>();

            // S'il n'y en a pas on sort
            if (pListeProp.Count == 0)
                return;

            // On recherche la première propriété triable ET modifiable
            PropertyInfo pPropTriModifiable = null;
            foreach (PropertyInfo Prop in pListeProp)
            {
                Tri pAttTri = (Tri)Prop.GetCustomAttributes(typeof(Tri)).First();
                if (pAttTri.Modifiable == true)
                {
                    pPropTriModifiable = Prop;
                    break;
                }
            }

            // S'il n'y en a pas, on sort aussi
            if (pPropTriModifiable == null)
                return;

            // Si tout est ok, on reindexe chaque Element avec sa position dans la liste + 1 pour ne pas avoir de 0
            foreach (T Element in this)
            {
                pPropTriModifiable.SetValue(Element, Convert.ChangeType(this.IndexOf(Element) + 1, pPropTriModifiable.PropertyType), null);
            }
        }

        public ListeObservable()
            : base()
        {
        }

        public ListeObservable(ListeObservable<T> Liste)
        {
            foreach (T item in Liste)
                base.Add(item);
        }

        public void Ajouter(T Item, Boolean Debut = false)
        {
            if (Contains(Item)) return;

            if (Debut)
                base.Insert(0, Item);
            else
                base.Add(Item);
        }

        public new void Add(T Item)
        {
            if (Contains(Item)) return;

            base.Add(Item);
        }

        public ListeObservable(IEnumerable<T> Liste)
            : this()
        {
            foreach (var item in Liste)
                this.Add(item);
        }
    }

    public class ListeAvecTitre<T> : ObservableCollection<T>
    {
        private String _Intitule = "";

        public String Intitule
        {
            get { return _Intitule; }
            set { _Intitule = value; }
        }

        public ListeAvecTitre() { }

        public ListeAvecTitre(String intitule) { _Intitule = intitule; }

        public void Importer(ICollection<T> collection)
        {
            foreach (T obj in collection)
                Add(obj);
        }
    }
}
