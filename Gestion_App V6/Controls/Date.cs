using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Gestion
{
    public partial class Date : ControlBase
    {

        public Boolean Editable
        {
            get { return (Boolean)GetValue(EditableDP); }
            set { SetValue(EditableDP, value); }
        }

        public static readonly DependencyProperty EditableDP =
            DependencyProperty.Register("Editable", typeof(Boolean),
              typeof(Date), new PropertyMetadata(null));

        public Boolean Intitule
        {
            get { return (Boolean)GetValue(IntituleDP); }
            set { SetValue(IntituleDP, value); }
        }

        public static readonly DependencyProperty IntituleDP =
            DependencyProperty.Register("Intitule", typeof(Boolean),
              typeof(Date), new PropertyMetadata(null));

        public Boolean Info
        {
            get { return (Boolean)GetValue(InfosDP); }
            set { SetValue(InfosDP, value); }
        }

        public static readonly DependencyProperty InfosDP =
            DependencyProperty.Register("Info", typeof(Boolean),
              typeof(Date), new PropertyMetadata(null));

        public object Valeur
        {
            get { return (object)GetValue(ValeurDP); }
            set { SetValue(ValeurDP, value); }
        }

        public static readonly DependencyProperty ValeurDP =
            DependencyProperty.Register("Valeur", typeof(object),
              typeof(Date), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        String Valeur_Objet = "";
        String Valeur_Propriete = "";
        String Valeur_TypePropriete = "";

        private void MajValeur()
        {
            if (Editable == true)
            {
                xValeur.Visibility = Visibility.Visible;
                xValeur.Background = Brushes.White;
                xValeur.IsHitTestVisible = true;
                xAfficher.Visibility = Visibility.Collapsed;
            }
            else
            {
                xValeur.Visibility = Visibility.Collapsed;
                xValeur.Background = Brushes.Transparent;
                xValeur.BorderThickness = new Thickness(0);
                xValeur.IsHitTestVisible = false;

                xAfficher.Visibility = Visibility.Visible;
                xAfficher.Background = Brushes.Transparent;
                xAfficher.BorderThickness = new Thickness(0);
                xAfficher.IsHitTestVisible = false;
            }

            if (Intitule == true)
                xIntitule.Visibility = Visibility.Visible;
            else
                xIntitule.Visibility = Visibility.Collapsed;

            String pIntitule = DicIntitules.Intitule(Valeur_Objet, Valeur_Propriete);
            xIntitule.Text = pIntitule + " :";

            if (Valeur != null && String.IsNullOrWhiteSpace(Valeur.ToString()) && (Editable == false))
                xBase.Visibility = System.Windows.Visibility.Collapsed;

            String ToolTip = DicIntitules.Info(Valeur_Objet, Valeur_Propriete);
            if (!String.IsNullOrWhiteSpace(ToolTip) && (Info == true))
                xBase.ToolTip = ToolTip;
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if ((e.Property == ValeurDP) && String.IsNullOrWhiteSpace(Valeur_Objet))
                InfosBinding(e.Property, ref Valeur_Objet, ref Valeur_Propriete, ref Valeur_TypePropriete);

            if (IsLoaded)
            {
                if (e.Property == ValeurDP)
                    MajValeur();
            }

            base.OnPropertyChanged(e);
        }

        public Date()
        {
            Loaded += Date_Loaded;
            InitializeComponent();
        }

        private void Date_Loaded(object sender, RoutedEventArgs e)
        {
            MajValeur();
        }
    }
}
