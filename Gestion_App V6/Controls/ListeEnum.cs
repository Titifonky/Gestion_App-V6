using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Gestion
{
    public class EnumToIntConverter : IValueConverter
    {
        private Type _T;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((value == null) || (!value.GetType().IsEnum))
                return null;

            _T = value.GetType();
            return (int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (_T != null)
                return Enum.Parse(_T, value.ToString());

            return null;
        }
    }

    public partial class ListeEnum : ControlBase
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
              typeof(ListeEnum), new PropertyMetadata(null));

        public Boolean Intitule
        {
            get { return (Boolean)GetValue(IntituleDP); }
            set { SetValue(IntituleDP, value); }
        }

        public static readonly DependencyProperty IntituleDP =
            DependencyProperty.Register("Intitule", typeof(Boolean),
              typeof(ListeEnum), new PropertyMetadata(null));

        public object Valeur
        {
            get { return (object)GetValue(ValeurDP); }
            set { SetValue(ValeurDP, value); }
        }

        public static readonly DependencyProperty ValeurDP =
            DependencyProperty.Register("Valeur", typeof(object),
              typeof(ListeEnum), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {

            if (e.Property == ValeurDP)
            {
                if (Editable == true)
                {
                    xValeur.Visibility = System.Windows.Visibility.Visible;
                    xValeur.IsHitTestVisible = true;
                    xValeur.Background = Brushes.White;
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

                String Objet = "";
                String Propriete = "";
                String TypePropriete = "";

                if (InfosBinding(e.Property, ref Objet, ref Propriete, ref TypePropriete))
                {
                    String pIntitule = DicIntitules.Intitule(Objet, Propriete);
                    xIntitule.Text = pIntitule + " :";
                    xValeur.ItemsSource = DicIntitules.Enum(TypePropriete);

                    if (String.IsNullOrWhiteSpace(Valeur.ToString()) && (Editable == false))
                        xBase.Visibility = System.Windows.Visibility.Collapsed;

                    String ToolTip = DicIntitules.Info(Objet, Propriete);
                    if (!String.IsNullOrWhiteSpace(ToolTip))
                        xBase.ToolTip = ToolTip;
                }
            }

            base.OnPropertyChanged(e);
        }

        public ListeEnum()
        {
            InitializeComponent();

            if (Intitule == true)
                xIntitule.Visibility = System.Windows.Visibility.Visible;
            else
                xIntitule.Visibility = System.Windows.Visibility.Collapsed;

            if (Editable == true)
                xValeur.IsHitTestVisible = true;
            else
            {
                xValeur.IsHitTestVisible = false;
                xValeur.ToolTip = null;
            }
        }
    }
}
