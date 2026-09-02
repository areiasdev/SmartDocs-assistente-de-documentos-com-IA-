namespace SmartDocs.Web.Interfaces;

public interface IPdfTextExtractor
{
    string ExtractText(string filePath);
}