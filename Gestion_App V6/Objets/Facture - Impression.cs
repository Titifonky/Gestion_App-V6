using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Gestion
{
    public partial class Facture : ObjetGestion
    {
        public FlowDocument Impression()
        {
            FlowDocument Doc = new FlowDocument();

            Societe pS = Devis.Client.Societe;

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
            Thickness TK = new Thickness(0, 0, 0, 10);
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

            FS = 13; TK = new Thickness(0, 0, 0, 20);
            PrgThick("Devis n°  " + this.Devis.Ref);

            FS = 16; TK = new Thickness(0, 0, 0, 20);
            PrgThick("Facture n°  " + this.Ref);

            FS = 13; TK = new Thickness(0, 0, 0, 10);
            PrgThick(this.Devis.Adresse_Client.Intitule);

            Prg(this.Devis.Adresse_Client.Adresse);
            Prg(this.Devis.Adresse_Client.Cp + " " + this.Devis.Adresse_Client.Ville);

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
            Pr.Margin = new Thickness(0, 0, 0, 10);
            Section.Blocks.Add(Pr);

            Pr = new Paragraph(new Run(Devis.Description));
            Pr.FontSize = 13;
            Pr.FontWeight = FontWeights.Bold;
            Pr.Padding = new Thickness(10, 0, 0, 0);
            Section.Blocks.Add(Pr);

            if (this.Imprimer_Commentaires && !String.IsNullOrWhiteSpace(this.Commentaires))
            {
                R2 = new TableRow();
                RgPrincipal.Rows.Add(R2);

                Section = new TableCell();
                Section.Padding = new Thickness(10);
                Section.BorderBrush = Brushes.Black;
                Section.BorderThickness = new Thickness(1);

                R2.Cells.Add(Section);

                Pr = new Paragraph(new Run(this.Commentaires));
                Pr.FontSize = 13;
                Pr.FontWeight = FontWeights.Bold;
                Pr.Padding = new Thickness(10, 0, 0, 0);
                Section.Blocks.Add(Pr);
            }


            //-------------------------------------------
            //Rtmp = new TableRow();
            //RgPrincipal.Rows.Add(Rtmp);

            //Section = new TableCell();
            //Section.Padding = new Thickness(0);

            //Rtmp.Cells.Add(Section);
            //------------------------------------------

            if (!String.IsNullOrWhiteSpace(Devis.Info))
            {
                R3 = new TableRow();
                RgPrincipal.Rows.Add(R3);

                Section = new TableCell();
                Section.Padding = new Thickness(10);
                Section.BorderBrush = Brushes.Black;
                Section.BorderThickness = new Thickness(1);

                R3.Cells.Add(Section);

                Pr = new Paragraph(new Run(Devis.Info));
                Pr.FontSize = 13;
                Pr.FontWeight = FontWeights.Bold;
                Pr.Padding = new Thickness(10, 0, 0, 0);
                Section.Blocks.Add(Pr);
            }

            //-------------------------------------------
            //Rtmp = new TableRow();
            //RgPrincipal.Rows.Add(Rtmp);

            //Section = new TableCell();
            //Section.Padding = new Thickness(0);

            //Rtmp.Cells.Add(Section);
            //------------------------------------------

            R4 = new TableRow();
            RgPrincipal.Rows.Add(R4);

            Section = new TableCell();
            Section.Padding = new Thickness(10);
            Section.BorderBrush = Brushes.Black;
            Section.BorderThickness = new Thickness(1);

            R4.Cells.Add(Section);

            foreach (Ligne_Facture L in this.ListeLigneFacture)
            {
                if (L.Statut)
                {
                    TA = TextAlignment.Left;

                    Poste P = L.Poste;

                    Pr = new Paragraph(new Run(P.Titre));
                    Pr.FontSize = 16;
                    Pr.FontWeight = FontWeights.Bold;
                    Pr.TextDecorations = TextDecorations.Underline;
                    Pr.Margin = new Thickness(0, 0, 0, 10);
                    Section.Blocks.Add(Pr);

                    T = new Table();
                    T.Padding = new Thickness(0);
                    T.Margin = new Thickness(0, 0, 0, 30);

                    C = new TableColumn();
                    C.Width = new GridLength(40, GridUnitType.Star);
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

                    if (L.Imprimer_Description)
                    {
                        FS = 13; FW = FontWeights.Normal; TK = new Thickness(0, 0, 0, 10);
                        Prg(P.Description);
                    }

                    if (!String.IsNullOrWhiteSpace(L.Description))
                    {
                        FS = 13; FW = FontWeights.Normal; TK = new Thickness(0, 0, 0, 10);
                        Prg(L.Description);
                    }

                    R.Cells.Add(Cl);

                    Cl = new TableCell();
                    Cl.Padding = new Thickness(10, 0, 0, 0);
                    Cl.BorderBrush = Brushes.Black;
                    Cl.BorderThickness = new Thickness(1, 0, 0, 0);
                    Cl.TextAlignment = TextAlignment.Right;

                    TA = TextAlignment.Right;
                    FS = 15; FW = FontWeights.Bold;

                    switch (L.CalculLigne_Facture)
                    {
                        case CalculLigne_Facture_e.cQuantite:
                            if (L.Qte != 1)
                            {
                                FS = 13; FW = FontWeights.Normal;
                                Prg(DicIntitules.Intitule("Ligne_Facture", "Qte") + " : " + L.Qte + " " + L.Unite);

                                Prg(DicIntitules.Intitule("Ligne_Facture", "Ht_Unitaire") + " : " + L.Ht_Unitaire + " " + DicIntitules.Unite("Ligne_Facture", "Ht_Unitaire"));
                            }

                            FS = 15; FW = FontWeights.Bold;
                            if (L.Qte != 1)
                            {
                                Prg("Total : " + L.Ht + " " + DicIntitules.Unite("Ligne_Facture", "Ht"));
                            }
                            else
                            {
                                Prg(L.Ht + " " + DicIntitules.Unite("Ligne_Facture", "Ht"));
                            }
                            break;

                        case CalculLigne_Facture_e.cPourcentageUnitaire:
                            FS = 13; FW = FontWeights.Normal;
                            Prg(DicIntitules.Intitule("Ligne_Facture", "Ht_Unitaire") + " : " + L.Ht_Unitaire + " " + DicIntitules.Unite("Ligne_Facture", "Ht_Unitaire"));
                            Prg(DicIntitules.Intitule("Ligne_Facture", "Qte") + " : " + L.Qte + " " + L.Unite);

                            FS = 15; FW = FontWeights.Bold;
                            Prg("Total : " + L.Ht + " " + DicIntitules.Unite("Ligne_Facture", "Ht"));

                            break;
                        case CalculLigne_Facture_e.cPourcentageTotal:
                            FS = 13; FW = FontWeights.Normal;
                            Prg(DicIntitules.Intitule("Ligne_Facture", "Ht") + " : " + L.Ht_Unitaire + " " + DicIntitules.Unite("Ligne_Facture", "Ht"));
                            Prg(DicIntitules.Intitule("Ligne_Facture", "Qte") + " : " + L.Qte + " " + L.Unite);

                            FS = 15; FW = FontWeights.Bold;
                            Prg("Total : " + L.Ht + " " + DicIntitules.Unite("Ligne_Facture", "Ht"));

                            break;
                        default:
                            break;
                    }

                    TA = TextAlignment.Left;

                    R.Cells.Add(Cl);

                    Section.Blocks.Add(T);
                }
            }

            //-------------------------------------------
            //Rtmp = new TableRow();
            //RgPrincipal.Rows.Add(Rtmp);

            //Section = new TableCell();
            //Section.Padding = new Thickness(0);

            //Rtmp.Cells.Add(Section);
            //------------------------------------------

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
            }

            R.Cells.Add(Cl);

            Cl = new TableCell();
            Cl.Padding = new Thickness(10, 0, 0, 0);
            Cl.BorderBrush = Brushes.Black;
            Cl.BorderThickness = new Thickness(1, 0, 0, 0);

            FS = 15; FW = FontWeights.Bold; TK = new Thickness(0, 0, 0, 10); TA = TextAlignment.Left;
            PrgThick(DicIntitules.Intitule("Facture", "Prix_Ht"));

            PrgThick(DicIntitules.Intitule("Devis", "Tva_Pct") + " " + Devis.Tva_Pct + DicIntitules.Unite("Devis", "Tva_Pct"));

            PrgThick(DicIntitules.Intitule("Facture", "Prix_Ttc"));

            R.Cells.Add(Cl);

            Cl = new TableCell();
            Cl.Padding = new Thickness(10, 0, 0, 0);
            Cl.BorderBrush = Brushes.Black;
            Cl.BorderThickness = new Thickness(0);

            TA = TextAlignment.Right;
            PrgThick(this.Prix_Ht + DicIntitules.Unite("Facture", "Prix_Ht"));

            PrgThick(this.Tva + DicIntitules.Unite("Facture", "Tva"));

            PrgThick(this.Prix_Ttc + DicIntitules.Unite("Facture", "Prix_Ttc"));

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

            Pr = new Paragraph(new Run(pS.Pied));
            Pr.FontSize = 13;
            Pr.TextAlignment = TextAlignment.Center;
            Section.Blocks.Add(Pr);

            //-------------------------------------------

            return Doc;
        }
    }
}
