namespace SmartDocs.Web.Models;

public class Document
{
    public int Id { get; init; }
    public string PublicId { get; init; } = Guid.NewGuid().ToString();
    public string FileName { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public DateTimeOffset UploadedAt { get; init; } = DateTimeOffset.UtcNow;
    public string StoragePath { get; init; } = string.Empty;
}