using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entity.Entities;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Entity.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly GpTalkDbContext _context;

        public MessageRepository(GpTalkDbContext context)
        {
            _context = context;
        }

        public async Task<Message> AddMessageAsync(int userId, long chatId, string message, ChatMessageRole role)
        {
            var result = await _context.Messages.AddAsync(new Message
            {
                UserId = userId,
                ChatMessageRole = ChatMessageRole.User,
                ChatId = chatId,
                Text = message
            });

            return result.Entity;
        }

        public async Task ClearAllMessagesAsync(long telegramUserId)
        {
            var userFromDb = await _context.Users.SingleAsync(u => u.TelegramUserId == telegramUserId);
            _context.Messages.RemoveRange(_context.Messages.Where(m => m.UserId == userFromDb.Id));
        }

        public IEnumerable<Message> GetLastMessages(int userId, int count)
        {
            return _context.Messages.Where(m => m.UserId == userId)
                .OrderByDescending(m => m.Id)
                .Take(10);
        }
    }
}
