using System;
using System.ComponentModel;
using System.Printing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Xps;

namespace Gestion
{
    /// <summary>
    /// Logique d'interaction pour Window1.xaml
    /// </summary>
    public partial class SelectionnerClient : Window
    {

        private void TextBox_ToucheEntreeUpdate(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tBox = (TextBox)sender;
                DependencyProperty prop = TextBox.TextProperty;

                BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
                if (binding != null) { binding.UpdateSource(); }
            }
        }

        private ModifierDevis _Dlgt = null;
        private Devis _DevisBase = null;
        private ListBox _Box = null;

        private ListeObservable<Client> _Liste = null;
        private RechercheTexte<Client> _Filtre = null;

        public SelectionnerClient(Devis DevisBase, ListBox Box, ModifierDevis Dlgt)
        {
            InitializeComponent();

            _Dlgt = Dlgt;
            _DevisBase = DevisBase;
            _Box = Box;

            _Liste = new ListeObservable<Client>(DevisBase.Client.Societe.ListeClient);
            _Objet = _Liste[0];

            xSelectionnerClient.DataContext = this;
            xSelectionnerClient.ItemsSource = _Liste;

            _Filtre = new RechercheTexte<Client>(xSelectionnerClient);

            xFiltrerClient.DataContext = _Filtre;
        }

        private void Valider(object sender, RoutedEventArgs e)
        {
            _Dlgt(Objet, _DevisBase, _Box);
            this.Close();
        }

        private void Annuler(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private Client _Objet;

        public Client Objet
        {
            get
            { return _Objet; }

            private set
            {
                _Objet = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] String NomProp = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(NomProp));
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.DragMove();
            }
            catch (Exception) { }
        }
    }
}
