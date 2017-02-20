using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Gestion
{
    public partial class Devis : ObjetGestion
    {

        public FlowDocument Impression()
        {

            FlowDocument Doc = new FlowDocument();

            Societe pS = Client.Societe;

            Doc.FontFamily = new FontFamily("Calibri");

            Table Tprincipale;
            Table T;
            TableColumn C;
            TableRowGroup RgPrincipal;
            TableRowGroup Rg;
            TableRow R1;
            TableRow R2;
            TableRow R3;
            TableRow R4;
            TableRow R5;
            TableRow R6;
            TableRow R;
            TableRow Rtmp;
            TableCell Section;
            
            Paragraph Pr;
            
            TableCell Cl = null;
            Double FS = 13;
            FontWeight FW = FontWeights.Normal;
            Thickness TK = new Thickness(0,0,0,10);
            TextAlignment TA = TextAlignment.Left;

            // Mise en page d'un paragraphe
            Action<Object> PrgThick = delegate(Object Chaine)
            {
                Pr = new Paragraph(new Run(Chaine.ToString()));
                Pr.FontSize = FS;
                Pr.FontWeight = FW;
                Pr.Margin = TK;
                Pr.TextAlignment = TA;
                Cl.Blocks.Add(Pr);
            };

            Action<Object> Prg = delegate(Object Chaine)
            {
                Pr = new Paragraph(new Run(Chaine.ToString()));
                Pr.FontSize = FS;
                Pr.FontWeight = FW;
                Pr.TextAlignment = TA;
                Cl.Blocks.Add(Pr);
            };


            Tprincipale = new Table();
            Tprincipale.Padding = new Thickness(0);

            Tprincipale.Columns.Add(new TableColumn());

            RgPrincipal = new TableRowGroup();
            Tprincipale.RowGroups.Add(RgPrincipal);

            Doc.Blocks.Add(Tprincipale);

            //-------------------------------------------

            R1 = new TableRow();
            RgPrincipal.Rows.Add(R1);

            Section = new TableCell();
            Section.Padding = new Thickness(10);
            Section.BorderBrush = Brushes.Black;
            Section.BorderThickness = new Thickness(1);

            R1.Cells.Add(Section);

            T = new Table();
            T.Padding = new Thickness(0);
            T.Margin = new Thickness(0);

            C = new TableColumn();
            C.Width = new GridLength(20, GridUnitType.Star);
            T.Columns.Add(C);

            C = new TableColumn();
            C.Width = new GridLength(10, GridUnitType.Star);
            T.Columns.Add(C);

            Rg = new TableRowGroup();
            T.RowGroups.Add(Rg);

            R = new TableRow();
            Rg.Rows.Add(R);

            Cl = new TableCell();
            Cl.Padding = new Thickness(0, 0, 10, 0);

            FS = 24; FW = FontWeights.Bold;
            Prg(pS.Statut);

            FS = 16; TK = new Thickness(0, 0, 0, 10);
            PrgThick(pS.Denomination);

            FS = 13;
            Prg(pS.Adresse);

            PrgThick(pS.Cp + " " + pS.Ville);

            Prg(pS.Tel_Mobile);

            Prg(pS.Email);

            R.Cells.Add(Cl);

            Cl = new TableCell();
            Cl.Padding = new Thickness(10, 0, 0, 0);
            Cl.BorderBrush = Brushes.Black;
            Cl.BorderThickness = new Thickness(1, 0, 0, 0);

            FS = 11; TK = new Thickness(0, 0, 0, 10);
            PrgThick(this.Date.ToString("dd/MM/yyyy"));

            FS = 16; TK = new Thickness(0, 0, 0, 20);
            PrgThick("Devis n°  " + this.Ref);

            FS = 13; TK = new Thickness(0, 0, 0, 10);
            PrgThick(this.Adresse_Client.Intitule);

            Prg(this.Adresse_Client.Adresse);
            Prg(this.Adresse_Client.Cp + " " + this.Adresse_Client.Ville);

            R.Cells.Add(Cl);

            Section.Blocks.Add(T);

            //-------------------------------------------
            Rtmp = new TableRow();
            RgPrincipal.Rows.Add(Rtmp);

            Section = new TableCell();
            Section.Padding = new Thickness(0);

            Rtmp.Cells.Add(Section);
            //------------------------------------------

            R2 = new TableRow();
            RgPrincipal.Rows.Add(R2);

            Section = new TableCell();
            Section.Padding = new Thickness(10);
            Section.BorderBrush = Brushes.Black;
            Section.BorderThickness = new Thickness(1);

            R2.Cells.Add(Section);

            Pr = new Paragraph(new Run("Objet :"));
            Pr.FontSize = 13;
            Pr.FontWeight = FontWeights.Bold;
            Pr.Margin = new Thickness(0, 0, 0, 2);
            Section.Blocks.Add(Pr);

            Pr = new Paragraph(new Run(this.Description));
            Pr.FontSize = 13;
            Pr.FontWeight = FontWeights.Bold;
            Pr.Padding = new Thickness(10, 0, 0, 0);
            Section.Blocks.Add(Pr);

            if (!String.IsNullOrWhiteSpace(this.Info))
            {
                R3 = new TableRow();
                RgPrincipal.Rows.Add(R3);

                Section = new TableCell();
                Section.Padding = new Thickness(10);
                Section.BorderBrush = Brushes.Black;
                Section.BorderThickness = new Thickness(1);

                R3.Cells.Add(Section);

                Pr = new Paragraph(new Run(this.Info));
                Pr.FontSize = 13;
                Pr.FontWeight = FontWeights.Bold;
                Pr.Padding = new Thickness(10, 0, 0, 0);
                Section.Blocks.Add(Pr);

            }

            R4 = new TableRow();
            RgPrincipal.Rows.Add(R4);

            Section = new TableCell();
            Section.Padding = new Thickness(10);
            Section.BorderBrush = Brushes.Black;
            Section.BorderThickness = new Thickness(1);

            R4.Cells.Add(Section);

            foreach (Poste P in this.ListePoste)
            {
                if (!P.Statut)
                    continue;

                Pr = new Paragraph(new Run(P.Titre));
                Pr.FontSize = 16;
                Pr.FontWeight = FontWeights.Bold;
                Pr.TextDecorations = TextDecorations.Underline;
                Pr.Margin = new Thickness(0, 0, 0, 5);
                Section.Blocks.Add(Pr);

                T = new Table();
                T.Padding = new Thickness(0);
                T.Margin = new Thickness(0, 0, 0, 30);

                C = new TableColumn();
                C.Width = new GridLength(40, GridUnitType.Star);
                T.Columns.Add(C);

                C = new TableColumn();
                C.Width = new GridLength(15, GridUnitType.Star);
                T.Columns.Add(C);

                Rg = new TableRowGroup();
                T.RowGroups.Add(Rg);



                R = new TableRow();
                Rg.Rows.Add(R);

                Cl = new TableCell();
                Cl.Padding = new Thickness(10, 0, 10, 0);

                FS = 13; FW = FontWeights.Normal; TK = new Thickness(0, 0, 0, 5);
                Prg(P.Description);

                R.Cells.Add(Cl);

                Cl = new TableCell();
                Cl.Padding = new Thickness(10, 0, 0, 0);
                Cl.BorderBrush = Brushes.Black;
                Cl.BorderThickness = new Thickness(1, 0, 0, 0);
                Cl.TextAlignment = TextAlignment.Right;

                FS = 13; FW = FontWeights.Normal; TK = new Thickness(0, 0, 0, 5); TA = TextAlignment.Right;

                if (P.Qte != 1)
                {
                    String pUnite = P.Unite;
                    if (!String.IsNullOrWhiteSpace(pUnite))
                        pUnite = "\\" + pUnite;

                    Prg(DicIntitules.Intitule("Poste", "Qte") + " : " + P.Qte + " " + P.Unite);
                    Prg(DicIntitules.Intitule("Poste", "Prix_Unitaire") + " : " + P.Prix_Unitaire  + " " + DicIntitules.Unite("Poste", "Prix_Unitaire") + pUnite);
                }

                FS = 15; FW = FontWeights.Bold;
                if (P.Qte != 1)
                    Prg("Total : " + P.Prix_Ht + " " + DicIntitules.Unite("Poste", "Prix_Ht"));
                else
                    Prg(P.Prix_Ht + " " + DicIntitules.Unite("Poste", "Prix_Ht"));

                TA = TextAlignment.Left;

                Cl.Blocks.Add(Pr);

                R.Cells.Add(Cl);

                Section.Blocks.Add(T); 

            }

            R5 = new TableRow();
            RgPrincipal.Rows.Add(R5);

            Section = new TableCell();
            Section.Padding = new Thickness(10);
            Section.BorderBrush = Brushes.Black;
            Section.BorderThickness = new Thickness(1);

            R5.Cells.Add(Section);

            T = new Table();
            T.Padding = new Thickness(0);
            T.Margin = new Thickness(0);

            C = new TableColumn();
            C.Width = new GridLength(20, GridUnitType.Star);
            T.Columns.Add(C);

            C = new TableColumn();
            C.Width = new GridLength(10, GridUnitType.Star);
            T.Columns.Add(C);

            C = new TableColumn();
            C.Width = new GridLength(5, GridUnitType.Star);
            T.Columns.Add(C);

            Rg = new TableRowGroup();
            T.RowGroups.Add(Rg);

            R = new TableRow();
            Rg.Rows.Add(R);

            Cl = new TableCell();
            Cl.Padding = new Thickness(0, 0, 10, 0);

            

            if (!String.IsNullOrWhiteSpace(this.Conditions))
            {

                FS = 16; FW = FontWeights.Bold; TK = new Thickness(0, 0, 0, 5); TA = TextAlignment.Left;
                PrgThick("Modalités de règlement : ");

                FS = 13; FW = FontWeights.Bold; TK = new Thickness(0, 0, 0, 5);
                Prg(this.Conditions);

                if (this.Acompte != 0)
                {
                    Pr = new Paragraph(new Run("Acompte de : " + this.Acompte + DicIntitules.Unite("Devis", "Acompte")));
                    Pr.FontSize = 13;
                    Pr.FontWeight = FontWeights.Bold;
                    Cl.Blocks.Add(Pr);
                }
            }

            R.Cells.Add(Cl);

            Cl = new TableCell();
            Cl.Padding = new Thickness(10, 0, 0, 0);
            Cl.BorderBrush = Brushes.Black;
            Cl.BorderThickness = new Thickness(1, 0, 0, 0);

            FS = 15; FW = FontWeights.Bold; TK = new Thickness(0, 0, 0, 10); TA = TextAlignment.Left;
            PrgThick(DicIntitules.Intitule("Devis", "Prix_Ht"));

            PrgThick(DicIntitules.Intitule("Devis", "Tva_Pct") + " " + this.Tva_Pct + DicIntitules.Unite("Devis", "Tva_Pct"));

            PrgThick(DicIntitules.Intitule("Devis", "Prix_Ttc"));

            R.Cells.Add(Cl);

            Cl = new TableCell();
            Cl.Padding = new Thickness(10, 0, 0, 0);
            Cl.BorderBrush = Brushes.Black;
            Cl.BorderThickness = new Thickness(0);

            TA = TextAlignment.Right;
            PrgThick(this.Prix_Ht + DicIntitules.Unite("Devis", "Prix_Ht"));

            PrgThick(this.Tva + DicIntitules.Unite("Devis", "Tva"));

            PrgThick(this.Prix_Ttc + DicIntitules.Unite("Devis", "Prix_Ttc"));

            R.Cells.Add(Cl);

            Section.Blocks.Add(T);

            //------------------------------------------

            R6 = new TableRow();
            RgPrincipal.Rows.Add(R6);

            Section = new TableCell();
            Section.Padding = new Thickness(10);
            Section.BorderBrush = Brushes.Black;
            Section.BorderThickness = new Thickness(1);

            R6.Cells.Add(Section);

            Pr = new Paragraph(new Run("BON POUR ACCORD, DATE ET SIGNATURE :"));
            Pr.FontSize = 16;
            Pr.FontWeight = FontWeights.Bold;
            Pr.Margin = new Thickness(0, 0, 0, 50);
            Section.Blocks.Add(Pr);

            Pr = new Paragraph(new Run(pS.Pied));
            Pr.FontSize = 13;
            Pr.TextAlignment = TextAlignment.Center;
            Section.Blocks.Add(Pr);

            //-------------------------------------------

            return Doc;
        }
    }
}