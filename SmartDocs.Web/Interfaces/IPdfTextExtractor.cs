namespace SmartDocs.Web.Services;

public interface IPdfTextExtractor
{
    string ExtractText(string filePath);
}