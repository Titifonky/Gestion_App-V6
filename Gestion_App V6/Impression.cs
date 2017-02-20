using System;
using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;
using System.Windows.Xps.Serialization;

namespace Gestion
{
    public static class MEP
    {
        public static DocumentPaginator Paginer(FlowDocument document)
        {
            // Clonage du document de base
            System.IO.MemoryStream s = new System.IO.MemoryStream();
            TextRange source = new TextRange(document.ContentStart, document.ContentEnd);
            source.Save(s, DataFormats.Xaml);
            FlowDocument copy = new FlowDocument();
            TextRange dest = new TextRange(copy.ContentStart, copy.ContentEnd);
            dest.Load(s, DataFormats.Xaml);

            // Création de la pagination
            DocumentPaginator paginator = ((IDocumentPaginatorSource)copy).DocumentPaginator;

            paginator.PageSize = new Size(790, 1115);

            Thickness t = new Thickness(20);  // PagePadding;
            copy.PagePadding = new Thickness(
                             Math.Max(0, t.Left),
                               Math.Max(0, t.Top),
                               Math.Max(0, t.Right),
                               Math.Max(0, t.Bottom));

            copy.ColumnWidth = double.PositiveInfinity;

            return paginator;
        }
    }

    public static class PDF
    {

        private static Boolean ByteArrayToFile(String _FileName, Byte[] _ByteArray)
        {
            try
            {
                // Open file for reading
                System.IO.FileStream _FileStream =
                   new System.IO.FileStream(_FileName, System.IO.FileMode.Create,
                                            System.IO.FileAccess.Write);
                // Writes a block of bytes to this stream using data from
                // a byte array.
                _FileStream.Write(_ByteArray, 0, _ByteArray.Length);

                // close file stream
                _FileStream.Close();

                return true;
            }
            catch (Exception _Exception)
            {
                // Error
                Console.WriteLine("Exception caught in process: {0}",
                                  _Exception.ToString());
            }

            // error occured, return false
            return false;
        }

        public static Boolean Creer(DocumentPaginator document, String Chemin)
        {
            if (String.IsNullOrWhiteSpace(Chemin)) return false;

            try
            {
                // Convertion du document en xps
                MemoryStream strm = new MemoryStream();
                Package pkg = Package.Open(strm, FileMode.OpenOrCreate);
                String pack = "pack://" + Guid.NewGuid().ToString() + ".xps";
                PackageStore.AddPackage(new Uri(pack), pkg);
                XpsDocument xpsDoc = new XpsDocument(pkg, CompressionOption.Maximum, pack);
                XpsSerializationManager xpsSM = new XpsSerializationManager(new XpsPackagingPolicy(xpsDoc), false);
                xpsSM.SaveAsXaml(document);

                // Enregistrement du xps dans un fichier tmp
                String tmpXpsFileName = Path.Combine(Path.GetTempPath(), "Gestion_" + DateTime.Now.Ticks + ".xps");
                XpsDocument xpsd = new XpsDocument(tmpXpsFileName, FileAccess.ReadWrite);
                System.Windows.Xps.XpsDocumentWriter xw = XpsDocument.CreateXpsDocumentWriter(xpsd);
                xw.Write(xpsDoc.GetFixedDocumentSequence());
                xpsd.Close();

                PdfSharp.Xps.XpsConverter.Convert(tmpXpsFileName, Chemin, 0);

                //// Convertion du xps en pdf
                //PdfDocument pdfDoc = new PdfDocument();
                //pdfDoc.LoadFromXPS(tmpXpsFileName);
                //Byte[] pdfFile;
                //using (MemoryStream stream = new MemoryStream())
                //{
                //    pdfDoc.SaveToStream(stream);
                //    pdfFile = stream.ToArray();
                //}

                //// Ecriture du fichier 
                //ByteArrayToFile(Chemin, pdfFile);

                //Byte[] pdfFile;
                //using (MemoryStream stream = new MemoryStream())
                //{
                //    PdfSharp.Xps.XpsModel.XpsDocument xpsDocument = PdfSharp.Xps.XpsModel.XpsDocument.Open(tmpXpsFileName);
                //    PdfSharp.Xps.XpsConverter.ConvertToPdfInMemory(xpsDocument, stream, 0);
                //    xpsDocument.Close();
                //    pdfFile = stream.ToArray();
                //}

                // Ecriture du fichier 
                //ByteArrayToFile(Chemin, pdfFile);

                // Suppression du fichier tmp
                File.Delete(tmpXpsFileName);

                return true;
            }
            catch { }

            return false;
        }

        public static Boolean Creer(FlowDocument document, String Chemin)
        {
            return Creer(MEP.Paginer(document), Chemin);
        }
    }
}
