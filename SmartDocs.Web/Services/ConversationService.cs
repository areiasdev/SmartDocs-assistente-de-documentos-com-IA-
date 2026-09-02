using Microsoft.EntityFrameworkCore;
using SmartDocs.Web.Data;
using SmartDocs.Web.Models;

namespace SmartDocs.Web.Services;

// Guardo o histórico de conversa por (utilizador, documento) na SQLite via EF Core,
// para sobreviver a reloads e a reiniciar a app — ao contrário dos embeddings do
// InMemoryVectorStore, que esses recrio sempre que volto a fazer ingest.
public class ConversationService
{
    private readonly ApplicationDbContext _db;
    public ConversationService(ApplicationDbContext db) => _db = db;

    public async Task AddMessageAsync(int conversationId, string role, string content, CancellationToken ct = default)
    {
        _db.Messages.Add(new Message { ConversationId = conversationId, Role = role, Content = content });
        await _db.SaveChangesAsync(ct);
    }

    // Cada documento que carrego tem a sua própria conversa por utilizador, assim
    // perguntar coisas sobre um PDF nunca mistura contexto/histórico com outro.
    public async Task<Conversation> GetOrCreateAsync(string userId, string documentId, CancellationToken ct = default)
    {
        var convo = await _db.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.DocumentId == documentId, ct);

        if (convo is null)
        {
            convo = new Conversation { UserId = userId, DocumentId = documentId };
            _db.Conversations.Add(convo);
            await _db.SaveChangesAsync(ct);
        }
        return convo;
    }
}