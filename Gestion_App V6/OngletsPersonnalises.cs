using System.Windows;
using System.Windows.Controls;

namespace Gestion
{
    /// <summary>
    /// ========================================
    /// .NET Framework 3.0 Custom Control
    /// ========================================
    ///
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:Gestion"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:Gestion;assembly=Gestion"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file. Note that Intellisense in the
    /// XML editor does not currently work on custom controls and its child elements.
    ///
    ///     <MyNamespace:Gestion/>
    ///
    /// </summary>
    public class OngletSupprimable : TabItem
    {
        static OngletSupprimable()
        {
            //This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
            //This style is defined in themes\generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata
                (
                    typeof(OngletSupprimable),
                    new FrameworkPropertyMetadata(typeof(OngletSupprimable))
                );
        }

        public static readonly RoutedEvent TabItemClosed_Event = EventManager.RegisterRoutedEvent
                                                                (
                                                                    "FermerOnglet",
                                                                    RoutingStrategy.Bubble,
                                                                    typeof(RoutedEventHandler),
                                                                    typeof(OngletSupprimable)
                                                                );

        public event RoutedEventHandler TabItem_Closed
        {
            add { AddHandler(TabItemClosed_Event, value); }
            remove { RemoveHandler(TabItemClosed_Event, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Button BoutonFermer = base.GetTemplateChild("PART_Close") as Button;
            if (BoutonFermer != null)
                BoutonFermer.Click += new System.Windows.RoutedEventHandler(BoutonFermer_Click);
        }

        void BoutonFermer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.RaiseEvent(new RoutedEventArgs(TabItemClosed_Event, this));
        }
    }
}
