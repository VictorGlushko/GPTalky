using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entity.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entity.Repositories
{
    public class UserRepository: IUserRepository
    {
        private readonly GpTalkDbContext _context;

        public UserRepository(GpTalkDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetUserByTelegramId(long id)
        {
            return await _context.Users.SingleOrDefaultAsync(u => u.TelegramUserId == id);
        }

        public async Task<User> AddUserAsync(string username, string firstName, string lastName, long telegramUserId)
        {
            var result = await _context.Users.AddAsync(new User
            {
                UserName = username,
                FirstName = firstName,
                LastName = lastName,
                TelegramUserId = telegramUserId,
            });

            return result.Entity;
        }
    }
}
