using Microsoft.EntityFrameworkCore;
using SmartDocs.Web.Data;
using SmartDocs.Web.Interfaces;
using SmartDocs.Web.Models;

namespace SmartDocs.Web.Services;

public class LocalDocumentService : IDocumentService
{
    private readonly string _root;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<LocalDocumentService> _logger;

    public LocalDocumentService(IWebHostEnvironment env, ApplicationDbContext db, ILogger<LocalDocumentService> logger)
    {
        _root = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(_root);
        _db = db;
        _logger = logger;
    }

    public async Task<Document> SaveAsync(string fileName, Stream content, CancellationToken ct = default)
    {
        var publicId = Guid.NewGuid().ToString();
        var storagePath = Path.Combine(_root, $"{publicId}.pdf");

        await using (var fs = File.Create(storagePath))
        {
            await content.CopyToAsync(fs, ct);
        }

        var doc = new Document
        {
            PublicId = publicId,
            FileName = fileName,
            SizeBytes = new FileInfo(storagePath).Length,
            StoragePath = storagePath
        };

        _db.Documents.Add(doc);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Documento guardado {DocId} ({FileName}, {Size} bytes)",
            doc.PublicId, fileName, doc.SizeBytes);
        return doc;
    }

    public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct = default)
        => await _db.Documents.AsNoTracking().ToListAsync(ct);
}