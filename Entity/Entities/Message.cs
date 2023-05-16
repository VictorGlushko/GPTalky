namespace Entity.Entities;

public class Message
{
    public int Id { get; set; }
    public string Text { get; set; }
    public long ChatId { get; set; }
    public ChatMessageRole ChatMessageRole { get; set; }
    public User User { get; set; }
    public int UserId { get; set; }
}