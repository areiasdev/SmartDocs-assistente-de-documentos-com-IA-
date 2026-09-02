using Microsoft.EntityFrameworkCore;
using SmartDocs.Web.Data;
using SmartDocs.Web.Models;

namespace SmartDocs.Web.Services;

public class ConversationService
{
    private readonly ApplicationDbContext _db;
    public ConversationService(ApplicationDbContext db) => _db = db;

    public async Task<Conversation> GetOrCreateAsync(string userId, CancellationToken ct = default)
    {
        var convo = await _db.Conversations
            .Include(c => c.Messages)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (convo is null)
        {
            convo = new Conversation { UserId = userId };
            _db.Conversations.Add(convo);
            await _db.SaveChangesAsync(ct);
        }
        return convo;
    }

    public async Task AddMessageAsync(int conversationId, string role, string content, CancellationToken ct = default)
    {
        _db.Messages.Add(new Message { ConversationId = conversationId, Role = role, Content = content });
        await _db.SaveChangesAsync(ct);
    }
}