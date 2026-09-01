namespace SmartDocs.Web.Models;

public class Document
{
    public int Id { get; set; }
    public string PublicId { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public string StoragePath { get; set; } = string.Empty;
}