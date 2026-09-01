using System.Text;
using UglyToad.PdfPig;

namespace SmartDocs.Web.Services;

public class PdfPigTextExtractor : IPdfTextExtractor
{
    public string ExtractText(string filePath)
    {
        var sb = new StringBuilder();
        using var pdf = PdfDocument.Open(filePath);
        foreach (var page in pdf.GetPages())
            sb.AppendLine(page.Text);
        return sb.ToString();
    }
}