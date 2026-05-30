using System;
using System.IO;
using System.Text;
using System.Linq;
namespace ServiceLayer.Services
{
    public class TextExtractionService
    {
        public async System.Threading.Tasks.Task<(bool success, string? text, string? errorMessage)> ExtractTextAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return (false, null, "File not found");
                var extension = Path.GetExtension(filePath).ToLower();
                return extension switch
                {
                    ".pdf" => ExtractFromPdf(filePath),
                    ".docx" => ExtractFromDocx(filePath),
                    ".pptx" => ExtractFromPptx(filePath),
                    _ => (false, null, $"Unsupported format: {extension}")
                };
            }
            catch (Exception ex)
            {
                return (false, null, $"Extraction failed: {ex.Message}");
            }
        }
        private (bool, string?, string?) ExtractFromPdf(string filePath)
        {
            try
            {
                var text = new StringBuilder();
                using (var pdfReader = new iText.Kernel.Pdf.PdfReader(filePath))
                using (var pdfDoc = new iText.Kernel.Pdf.PdfDocument(pdfReader))
                {
                    for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                    {
                        var page = pdfDoc.GetPage(i);
                        var content = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page);
                        text.AppendLine(content);
                    }
                }
                return text.Length == 0 ? (false, null, "No text") : (true, text.ToString(), null);
            }
            catch (Exception ex)
            {
                return (false, null, $"PDF: {ex.Message}");
            }
        }
        private (bool, string?, string?) ExtractFromDocx(string filePath)
        {
            try
            {
                var text = new StringBuilder();
                using (var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(filePath, false))
                {
                    if (doc.MainDocumentPart != null)
                    {
                        var body = doc.MainDocumentPart.Document.Body;
                        foreach (var para in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                        {
                            var txt = string.Join("", para.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
                            text.AppendLine(txt);
                        }
                    }
                }
                return text.Length == 0 ? (false, null, "No text") : (true, text.ToString(), null);
            }
            catch (Exception ex)
            {
                return (false, null, $"DOCX: {ex.Message}");
            }
        }
        private (bool, string?, string?) ExtractFromPptx(string filePath)
        {
            try
            {
                var text = new StringBuilder();
                using (var pptx = DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(filePath, false))
                {
                    var pPart = pptx.PresentationPart;
                    if (pPart != null && pPart.SlideParts != null)
                    {
                        foreach (var slidePart in pPart.SlideParts)
                        {
                            var slide = slidePart.Slide;
                            var shapes = slide?.CommonSlideData?.ShapeTree?.Descendants<DocumentFormat.OpenXml.Presentation.Shape>();
                            if (shapes != null)
                            {
                                foreach (var shape in shapes)
                                {
                                    if (shape.TextBody != null)
                                    {
                                        foreach (var para in shape.TextBody.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
                                        {
                                            var txt = string.Join("", para.Descendants<DocumentFormat.OpenXml.Drawing.Text>().Select(t => t.Text));
                                            if (!string.IsNullOrWhiteSpace(txt))
                                                text.AppendLine(txt);
                                        }
                                    }
                                }
                            }
                            text.AppendLine("---");
                        }
                    }
                }
                return text.Length == 0 ? (false, null, "No text") : (true, text.ToString(), null);
            }
            catch (Exception ex)
            {
                return (false, null, $"PPTX: {ex.Message}");
            }
        }
    }
}
