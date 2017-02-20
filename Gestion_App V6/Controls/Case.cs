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
        
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == IntituleDerriereDP)
            {
                String Objet = "";
                String Propriete = "";
                String TypePropriete = "";

                if (InfosBinding(e.Property, ref Objet, ref Propriete, ref TypePropriete))
                {
                    String pIntitule = DicIntitules.Intitule(Objet, Propriete);
                    if (IntituleDerriere == false)
                        pIntitule = pIntitule + " :";

                    xIntitule.Text = pIntitule;
                }
            }

            if (e.Property == ValeurDP)
            {
                if (Editable == true)
                {
                    xValeur.Visibility = System.Windows.Visibility.Visible;
                    xValeur.IsHitTestVisible = true;
                }
                else
                {
                    xValeur.Visibility = System.Windows.Visibility.Visible;
                    xValeur.IsHitTestVisible = false;
                    xValeur.ToolTip = null;
                }

                if (Intitule == true)
                    xIntitule.Visibility = System.Windows.Visibility.Visible;
                else
                    xIntitule.Visibility = System.Windows.Visibility.Collapsed;

                if(IntituleDerriere == true)
                {
                    Grid.SetColumn(xIntitule, 1);
                    Grid.SetColumn(xValeur, 0);
                    xIntitule.Margin = new Thickness(5, 0, 0, 0);
                }

                String Objet = "";
                String Propriete = "";
                String TypePropriete = "";

                if (InfosBinding(e.Property, ref Objet, ref Propriete, ref TypePropriete))
                {
                    String pIntitule = DicIntitules.Intitule(Objet, Propriete);
                    if (IntituleDerriere == false)
                        pIntitule = pIntitule + " :";

                    xIntitule.Text = pIntitule;

                    if (String.IsNullOrWhiteSpace(Valeur.ToString()) && (Editable == false))
                        xBase.Visibility = System.Windows.Visibility.Collapsed;


                    String ToolTip = DicIntitules.Info(Objet, Propriete);
                    if (!String.IsNullOrWhiteSpace(ToolTip))
                        xBase.ToolTip = ToolTip;
                }
            }

            base.OnPropertyChanged(e);
        }

        public Case()
        {
            InitializeComponent();
        }
    }
}
