using System.ComponentModel.DataAnnotations;

namespace Entity.Entities;

public class User
{
    public int Id { get; set; }
    
    [MaxLength(150)]
    public string? FirstName { get; set; }
    
    [MaxLength(150)]
    public string? LastName { get; set; }
    
    [MaxLength(150)]
    public string? UserName { get; set; }
    public long TelegramUserId { get; set; }
    public ICollection<Message> Messages { get; set;}
}