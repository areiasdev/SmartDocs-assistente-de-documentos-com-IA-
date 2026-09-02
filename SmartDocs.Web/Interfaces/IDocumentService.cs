using SmartDocs.Web.Models;

namespace SmartDocs.Web.Interfaces;

public interface IDocumentService
{
    Task<Document> SaveAsync(string fileName, Stream content, CancellationToken ct = default);
    Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct = default);
}