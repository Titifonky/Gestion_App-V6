using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Gestion
{
    public class RechercheTexte<T> : INotifyPropertyChanged
        where T : ObjetGestion
    {
        private Selector _Box = null;
        private List<Selector> _ListeBox = null;
        private String _Valeur;
        private CollectionView _Vue = null;
        private List<CollectionView> _ListeVue = null;
        private Predicate<Object> _Methode = null;

        public String Valeur
        {
            get
            { return _Valeur; }

            set
            {
                if (_Valeur != value)
                {
                    _Valeur = value;

                    if (_Box != null)
                    {
                        _Vue = (CollectionView)CollectionViewSource.GetDefaultView(_Box.ItemsSource);

                        if (_Vue == null) return;

                        if (_Vue.Filter != _Methode)
                            _Vue.Filter = _Methode;

                        _Vue.Refresh();
                        _Box.SelectedIndex = 0;
                    }

                    if (_ListeBox != null)
                    {
                        _ListeVue = new List<CollectionView>();
                        foreach (Selector Box in _ListeBox)
                        {

                            CollectionView Vue = (CollectionView)CollectionViewSource.GetDefaultView(Box.ItemsSource);
                            if (Vue == null) continue;

                            if (Vue.Filter != _Methode)
                                Vue.Filter = _Methode;

                            Vue.Refresh();
                            Box.SelectedIndex = 0;

                            _ListeVue.Add(Vue);
                        }
                    }
                    OnPropertyChanged();
                }
            }
        }

        public RechercheTexte(Selector Box)
        {
            _Box = Box;
            _Methode = new Predicate<object>(Filtre);
        }

        public RechercheTexte(List<Selector> ListeBox)
        {
            _ListeBox = ListeBox;
            _Methode = new Predicate<object>(Filtre);
        }

        private Boolean Filtre(object Source)
        {
            if (String.IsNullOrWhiteSpace(_Valeur))
                return true;

            T Obj = Source as T;

            try
            {
                if (Obj != null)
                {
                    String Chaine_Recherche = Valeur;
                    String Chaine_Prop = @"^.*";

                    if (Regex.IsMatch(Valeur, @"^\[.*\].*"))
                    {
                        Chaine_Recherche = Regex.Replace(Valeur, @"(^\[)(.*)(\])(.*)", "$4");
                        Chaine_Prop = Regex.Replace(Valeur, @"(^\[)(.*)(\])(.*)", "$2");
                    }
                    
                    foreach (PropertyInfo Prop in Bdd1.DicProprietes.ListePropriete(typeof(T)).Values)
                    {
                        if (Regex.IsMatch(Prop.Name.ToLower(), Chaine_Prop.ToLower()))
                            if (Regex.IsMatch(Prop.GetValue(Obj).ToString().RemoveDiacritics(), Chaine_Recherche, RegexOptions.IgnoreCase))
                                return true;
                    }
                }
            }
            catch
            {
                return true;
            }

            return false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] String NomProp = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(NomProp));
            }
        }
    }
}
