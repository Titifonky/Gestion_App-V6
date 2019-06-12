using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Gestion
{
    public partial class Case : ControlBase
    {
        protected void TextBox_KeyEnterUpdate(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tBox = (TextBox)sender;
                DependencyProperty prop = TextBox.TextProperty;

                BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
                if (binding != null) { binding.UpdateSource(); }
            }
        }

        public Boolean Editable
        {
            get { return (Boolean)GetValue(EditableDP); }
            set { SetValue(EditableDP, value); }
        }

        public static readonly DependencyProperty EditableDP =
            DependencyProperty.Register("Editable", typeof(Boolean),
              typeof(Case), new PropertyMetadata(null));

        public Boolean Intitule
        {
            get { return (Boolean)GetValue(IntituleDP); }
            set { SetValue(IntituleDP, value); }
        }

        public static readonly DependencyProperty IntituleDP =
            DependencyProperty.Register("Intitule", typeof(Boolean),
              typeof(Case), new PropertyMetadata(null));

        public Boolean IntituleDerriere
        {
            get { return (Boolean)GetValue(IntituleDerriereDP); }
            set { SetValue(IntituleDerriereDP, value); }
        }

        public static readonly DependencyProperty IntituleDerriereDP =
            DependencyProperty.Register("IntituleDerriere", typeof(Boolean),
              typeof(Case), new FrameworkPropertyMetadata(false));

        public object Valeur
        {
            get { return (object)GetValue(ValeurDP); }
            set { SetValue(ValeurDP, value); }
        }

        public static readonly DependencyProperty ValeurDP =
            DependencyProperty.Register("Valeur", typeof(object),
              typeof(Case), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        private void MajIntituleDerriere()
        {
            String pIntitule = DicIntitules.Intitule(IntituleDerriere_Objet, IntituleDerriere_Propriete);
            if (IntituleDerriere == false)
                pIntitule = pIntitule + " :";

            xIntitule.Text = pIntitule;
        }

        private void MajValeur()
        {
            if (Editable == true)
            {
                xValeur.Visibility = Visibility.Visible;
                xValeur.IsHitTestVisible = true;
            }
            else
            {
                xValeur.Visibility = Visibility.Visible;
                xValeur.IsHitTestVisible = false;
                xValeur.ToolTip = null;
            }

            if (Intitule == true)
                xIntitule.Visibility = Visibility.Visible;
            else
                xIntitule.Visibility = Visibility.Collapsed;

            if (IntituleDerriere == true)
            {
                Grid.SetColumn(xIntitule, 1);
                Grid.SetColumn(xValeur, 0);
                xIntitule.Margin = new Thickness(5, 0, 0, 0);
            }

            String pIntitule = DicIntitules.Intitule(Valeur_Objet, Valeur_Propriete);
            if (IntituleDerriere == false)
                pIntitule = pIntitule + " :";

            xIntitule.Text = pIntitule;

            if (String.IsNullOrWhiteSpace(Valeur.ToString()) && (Editable == false))
                xBase.Visibility = Visibility.Collapsed;


            String ToolTip = DicIntitules.Info(Valeur_Objet, Valeur_Propriete);
            if (!String.IsNullOrWhiteSpace(ToolTip))
                xBase.ToolTip = ToolTip;
        }

        String IntituleDerriere_Objet = "";
        String IntituleDerriere_Propriete = "";
        String IntituleDerriere_TypePropriete = "";

        String Valeur_Objet = "";
        String Valeur_Propriete = "";
        String Valeur_TypePropriete = "";

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if ((e.Property == IntituleDerriereDP) && String.IsNullOrWhiteSpace(IntituleDerriere_Objet))
                InfosBinding(e.Property, ref IntituleDerriere_Objet, ref IntituleDerriere_Propriete, ref IntituleDerriere_TypePropriete);

            if ((e.Property == ValeurDP) && String.IsNullOrWhiteSpace(Valeur_Objet))
                InfosBinding(e.Property, ref Valeur_Objet, ref Valeur_Propriete, ref Valeur_TypePropriete);

            if (IsLoaded)
            {
                if (e.Property == IntituleDerriereDP)
                    MajIntituleDerriere();

                if (e.Property == ValeurDP)
                    MajValeur();
            }

            base.OnPropertyChanged(e);
        }

        public Case()
        {
            Loaded += Case_Loaded;
            InitializeComponent();
        }

        private void Case_Loaded(object sender, RoutedEventArgs e)
        {
            MajIntituleDerriere();
            MajValeur();
        }
    }
}
