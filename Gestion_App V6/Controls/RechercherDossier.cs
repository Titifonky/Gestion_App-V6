using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Gestion
{
    public partial class RechercherDossier : ControlBase
    {

        private void ChercherDossier(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();

            dlg.Description = "Selectionnez le dossier...";

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                String Chemin = dlg.SelectedPath;
                Valeur = Chemin;
            }
        }

        public Boolean Editable
        {
            get { return (Boolean)GetValue(EditableDP); }
            set { SetValue(EditableDP, value); }
        }

        public static readonly DependencyProperty EditableDP =
            DependencyProperty.Register("Editable", typeof(Boolean),
              typeof(RechercherDossier), new PropertyMetadata(null));

        public Boolean Intitule
        {
            get { return (Boolean)GetValue(IntituleDP); }
            set { SetValue(IntituleDP, value); }
        }

        public static readonly DependencyProperty IntituleDP =
            DependencyProperty.Register("Intitule", typeof(Boolean),
              typeof(RechercherDossier), new PropertyMetadata(null));

        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentDP); }
            set { SetValue(TextAlignmentDP, value); }
        }

        public static readonly DependencyProperty TextAlignmentDP =
            DependencyProperty.Register("TextAlignment", typeof(TextAlignment),
              typeof(RechercherDossier), new FrameworkPropertyMetadata(TextAlignment.Left));

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationDP); }
            set { SetValue(OrientationDP, value); }
        }

        public static readonly DependencyProperty OrientationDP =
            DependencyProperty.Register("Orientation", typeof(Orientation),
              typeof(RechercherDossier), new FrameworkPropertyMetadata(Orientation.Horizontal));

        public Boolean Info
        {
            get { return (Boolean)GetValue(InfosDP); }
            set { SetValue(InfosDP, value); }
        }

        public static readonly DependencyProperty InfosDP =
            DependencyProperty.Register("Info", typeof(Boolean),
              typeof(RechercherDossier), new FrameworkPropertyMetadata(true));

        public object Valeur
        {
            get { return (object)GetValue(ValeurDP); }
            set { SetValue(ValeurDP, value); }
        }

        public static readonly DependencyProperty ValeurDP =
            DependencyProperty.Register("Valeur", typeof(object),
              typeof(RechercherDossier), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == ValeurDP)
            {
                if (Editable)
                {
                    xValeur.Visibility = System.Windows.Visibility.Visible;
                    xValeur.Background = Brushes.White;
                    xValeur.IsHitTestVisible = true;
                }
                else
                {
                    xValeur.Visibility = System.Windows.Visibility.Visible;
                    xValeur.Background = Brushes.Transparent;
                    xValeur.IsReadOnly = true;
                    xValeur.BorderThickness = new Thickness(0);
                    xValeur.IsHitTestVisible = false;
                }

                if (Orientation == System.Windows.Controls.Orientation.Horizontal)
                    DockPanel.SetDock(xIntitule, Dock.Left);
                else
                {
                    DockPanel.SetDock(xIntitule, Dock.Top);
                    xIntitule.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                }

                if (Intitule)
                    xIntitule.Visibility = System.Windows.Visibility.Visible;
                else
                {
                    xIntitule.Visibility = System.Windows.Visibility.Collapsed;
                    xIntitule.Margin = new Thickness(0);
                    xIntitule.Padding = new Thickness(0);
                }

                String Objet = "";
                String Propriete = "";
                String TypePropriete = "";

                if (InfosBinding(e.Property, ref Objet, ref Propriete, ref TypePropriete))
                {
                    String pIntitule = DicIntitules.Intitule(Objet, Propriete);
                    xIntitule.Text = pIntitule + " :";

                    if (String.IsNullOrWhiteSpace(Valeur.ToString()) && (Editable == false))
                        xBase.Visibility = System.Windows.Visibility.Collapsed;

                    String ToolTip = DicIntitules.Info(Objet, Propriete);
                    if (!String.IsNullOrWhiteSpace(ToolTip) && (Info == true))
                        xBase.ToolTip = ToolTip;
                }
            }

            base.OnPropertyChanged(e);
        }

        public RechercherDossier()
        {
            InitializeComponent();
        }
    }
}
