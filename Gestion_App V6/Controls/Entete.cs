using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Gestion
{
    public partial class Entete : ControlBase
    {
        public String NomPropriete
        {
            get { return (String)GetValue(NomProprieteDP); }
            set { SetValue(NomProprieteDP, value); }
        }

        public static readonly DependencyProperty NomProprieteDP =
            DependencyProperty.Register("NomPropriete", typeof(String),
              typeof(Entete), new FrameworkPropertyMetadata(null));

        public Dictionary<String, String> Liste
        {
            get { return (Dictionary<String, String>)GetValue(ListeProp); }
            set { SetValue(ListeProp, value); }
        }

        public static readonly DependencyProperty ListeProp =
            DependencyProperty.Register("Liste", typeof(Dictionary<String, String>),
              typeof(Entete), new FrameworkPropertyMetadata(null));

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == ListeProp)
            {
                if((Liste != null) && Liste.ContainsKey(NomPropriete))
                    xEntete.Text = Liste[NomPropriete];
            }

            base.OnPropertyChanged(e);
        }

        public Entete()
        {
            InitializeComponent();
        }
    }
}
