using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Gestion
{
    public class ControlBase : UserControl
    {
        protected Boolean InfosBinding(DependencyProperty DP, ref String Objet, ref String Propriete, ref String TypePropriete)
        {
            try
            {
                BindingExpression Binding = GetBindingExpression(DP);
                if ((Binding != null) && (Binding.ResolvedSource != null))
                {
                    Objet = Binding.ResolvedSource.GetType().Name;
                    Propriete = Binding.ResolvedSourcePropertyName;
                    TypePropriete = Binding.ResolvedSource.GetType().GetProperty(Propriete).PropertyType.Name;

                    if (!String.IsNullOrWhiteSpace(Objet) && !String.IsNullOrWhiteSpace(Propriete))
                        return true;
                }
            }
            catch { }
            return false;
        }
    }
}
