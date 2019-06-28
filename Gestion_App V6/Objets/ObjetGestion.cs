using LogDebugging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        {
            get { return _EstCharge; }
            set { _EstCharge = value; }
        }

        public Boolean EstPreCharge { get; set; } = false;

        protected int _No = 0;
        [Propriete, Max, Tri]
        public virtual int No
        {
            get { return _No; }
            set { Set(ref _No, value, this); }
        }

        protected String _Prefix_Utilisateur = "";
        [Propriete, NePasCopier]
        public virtual String Prefix_Utilisateur
        {
            get { return _Prefix_Utilisateur; }
            set { Set(ref _Prefix_Utilisateur, value, this); }
        }

        protected String _Ref = "";
        [Propriete, NePasCopier]
        public virtual String Ref
        {
            get { return _Ref; }
            set { Set(ref _Ref, value, this); }
        }

        protected virtual void MajRef(String reference = null)
        {
            if (String.IsNullOrWhiteSpace(reference))
                reference = Prefix_Utilisateur + No.ToString();

            if (reference != _Ref)
                Ref = reference;
        }

        protected Boolean _Editer = false;
        public virtual Boolean Editer
        {
            get { return _Editer; }
            set { Set(ref _Editer, value, this); }
        }

        public ObjetGestion()
        {
            T = this.GetType();
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
            catch (Exception e)
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
            if (EqualityComparer<U>.Default.Equals(field, value)) return false;

            field = value;
            OnPropertyChanged(propertyName);
            if (EstSvgDansLaBase)
                Bdd2.Maj(Objet, T, propertyName);
            return true;
        }

        protected bool SetObjetGestion<U, V>(ref U field, U value, V Objet, Boolean ForcerUpdate = false, [CallerMemberName]string propertyName = "")
            where U : ObjetGestion
            where V : ObjetGestion
        {
            Boolean test = true;
            if ((value == null) || (value != null && !value.EstCharge) || !Objet.EstCharge) test = false;

            if (ForcerUpdate || !EqualityComparer<U>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
                if (EstSvgDansLaBase)
                    Bdd2.Maj(Objet, T, propertyName);
            }

            return test;
        }

        protected bool SetListe<U>(ref U field, U value, [CallerMemberName]string propertyName = "")
        {
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] String NomProp = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(NomProp));
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

        public Boolean OptionsCharges { get; set; } = false;

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

        private Boolean _ItemsNotifyPropertyChanged = false;

        public Boolean ItemsNotifyPropertyChanged
        {
            get { return _ItemsNotifyPropertyChanged; }
            set
            {
                _ItemsNotifyPropertyChanged = value;
                if (value)
                {
                    foreach (var item in this)
                    {
                        var notifyItem = item as INotifyPropertyChanged;
                        if (notifyItem != null)
                            notifyItem.PropertyChanged += ItemPropertyChanged;
                    }
                }
                else
                {
                    foreach (var item in this)
                    {
                        var notifyItem = item as INotifyPropertyChanged;
                        if (notifyItem != null)
                            notifyItem.PropertyChanged -= ItemPropertyChanged;
                    }
                }
            }
        }

        public delegate int FonctionTriAscHandler(T a, T b);

        public event FonctionTriAscHandler TrierAsc;

        public delegate int FonctionTriDescHandler(T a, T b);

        public event FonctionTriDescHandler TrierDesc;

        public ListeObservable()
            : base()
        {
        }

        public ListeObservable(ListeObservable<T> Liste)
        {
            foreach (T item in Liste)
                base.Add(item);
        }

        public ListeObservable(IEnumerable<T> Liste)
            : this()
        {
            foreach (var item in Liste)
                this.Add(item);
        }

        public delegate void OnAjouterHandler(T obj, int? id);

        public event OnAjouterHandler OnAjouter;

        public delegate void OnSupprimerHandler(T obj, int? id);

        public event OnSupprimerHandler OnSupprimer;

        private int ChercherIndex(T Item)
        {
            int index = 0;
            if (TrierAsc != null)
            {
                for (index = this.Count - 1; index > -1; index--)
                {
                    if (TrierAsc(Item, this[index]) >= 0)
                        break;
                }
                index = Math.Min(this.Count - 1, index + 1);
            }
            else if(TrierDesc != null)
            {
                for (index = 0; index < this.Count; index++)
                {
                    if (TrierDesc(Item, this[index]) >= 0)
                        break;
                }

                index = Math.Max(0, index - 1);
                Log.Message("Max " + this.Count + "  INDEX :  " + index);
            }

            return index;
        }

        public void Ajouter(T Item, Boolean Debut = false)
        {
            if (Contains(Item)) return;

            if (TrierAsc != null || TrierDesc != null)
                base.Insert(ChercherIndex(Item), Item);
            else
            {
                if (Debut)
                    base.Insert(0, Item);
                else
                    base.Add(Item);
            }

            OnAjouter?.Invoke(Item, null);
        }

        public void Supprimer(T Item)
        {
            base.Remove(Item);

            OnSupprimer?.Invoke(Item, null);
        }

        public new void Add(T Item)
        {
            if (Contains(Item)) return;

            if (TrierAsc != null || TrierDesc != null)
                base.Insert(ChercherIndex(Item), Item);
            else
                base.Add(Item);

            OnAjouter?.Invoke(Item, null);
        }

        public new void Insert(int Index, T Item)
        {
            if (Contains(Item)) return;

            if (TrierAsc != null || TrierDesc != null)
                base.Insert(ChercherIndex(Item), Item);
            else
                base.Insert(Index, Item);

            OnAjouter?.Invoke(Item, Index);
        }

        public new void InsertItem(int Index, T Item)
        {
            if (Contains(Item)) return;

            if (TrierAsc != null || TrierDesc != null)
                base.Insert(ChercherIndex(Item), Item);
            else
                base.InsertItem(Index, Item);

            OnAjouter?.Invoke(Item, Index);
        }

        public new void Remove(T Item)
        {
            base.Remove(Item);

            OnSupprimer?.Invoke(Item, null);
        }

        public new void RemoveAt(int Index)
        {
            var Item = base[Index];

            base.RemoveAt(Index);

            OnSupprimer?.Invoke(Item, Index);
        }

        public new void RemoveItem(int Index)
        {
            var Item = base[Index];

            base.RemoveItem(Index);

            OnSupprimer?.Invoke(Item, Index);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (ItemsNotifyPropertyChanged && e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    var notifyItem = item as INotifyPropertyChanged;
                    if (notifyItem != null)
                        notifyItem.PropertyChanged += ItemPropertyChanged;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems)
                {
                    var notifyItem = item as INotifyPropertyChanged;
                    if (notifyItem != null)
                        notifyItem.PropertyChanged -= ItemPropertyChanged;
                }
            }

            base.OnCollectionChanged(e);
        }

        private void ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(ItemsNotifyPropertyChanged)
                base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
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
