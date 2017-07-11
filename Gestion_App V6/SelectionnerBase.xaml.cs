using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Gestion
{
    public partial class SelectionnerBase : Window
    {

        public SelectionnerBase(List<String> ListeBases)
        {
            InitializeComponent();

            xSelectionnerBase.DataContext = this;
            xSelectionnerBase.ItemsSource = ListeBases;
            xSelectionnerBase.SelectedIndex = 0;
        }

        private void Valider(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private String _NomBase;

        public String BaseSelectionnee
        {
            get
            { return _NomBase; }

            private set
            {
                _NomBase = value;
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
