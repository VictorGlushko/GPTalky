using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entity.Entities;

namespace Entity.Repositories
{
    public interface IMessageRepository
    {
        Task<Message> AddMessageAsync(int userId, long chatId, string message, ChatMessageRole role);
        Task ClearAllMessagesAsync(long telegramUserId);
        IEnumerable<Message> GetLastMessages(int userId, int count);
    }
}
