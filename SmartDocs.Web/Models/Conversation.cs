namespace SmartDocs.Web.Models;

public class Conversation
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";        // liga ao utilizador do Identity
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Message> Messages { get; set; } = new();
}

public class Message
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string Role { get; set; } = "";           // "user" ou "assistant" nao sei
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}