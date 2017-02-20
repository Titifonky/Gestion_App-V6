using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Gestion
{
    public partial class GridExpander : Expander
    {
        private Double SvgWidth = 0;

        private ColumnDefinition Colonne = null;

        private void Init()
        {
            int Index = Grid.GetColumn(this);
            FrameworkElement El = this as FrameworkElement;
            Grid G = El.Parent as Grid;
            Colonne = G.ColumnDefinitions[Index];
        }

        protected override void OnExpanded()
        {
            if (IsInitialized)
                Colonne.Width = new GridLength(SvgWidth);

            base.OnExpanded();
        }

        protected override void OnCollapsed()
        {
            if (IsInitialized)
            {
                SvgWidth = Colonne.ActualWidth;
                Colonne.Width = GridLength.Auto;
            }

            base.OnCollapsed();
        }

        public override void EndInit()
        {
            base.EndInit();
            Init();
        }
    }
}
