using SmartDocs.Web.Models;

namespace SmartDocs.Web.Services;

public class LocalDocumentService : IDocumentService
{
    private readonly string _root;
    private readonly ILogger<LocalDocumentService> _logger;
    private readonly List<Document> _index = new(); // índice temporário até termos Cosmos

    public LocalDocumentService(IWebHostEnvironment env, ILogger<LocalDocumentService> logger)
    {
        _root = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(_root);
        _logger = logger;
    }

    public async Task<Document> SaveAsync(string fileName, Stream content, CancellationToken ct = default)
    {
        var doc = new Document { FileName = fileName };
        doc.StoragePath = Path.Combine(_root, $"{doc.Id}.pdf");

        await using var fs = File.Create(doc.StoragePath);
        await content.CopyToAsync(fs, ct);
        doc.SizeBytes = fs.Length;

        _index.Add(doc);
        _logger.LogInformation("Documento guardado {DocId} ({FileName}, {Size} bytes)",
            doc.Id, fileName, doc.SizeBytes);
        return doc;
    }

    public Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Document>>(_index);
}