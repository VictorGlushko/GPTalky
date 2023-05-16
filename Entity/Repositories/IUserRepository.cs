using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entity.Entities;

namespace Entity.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetUserByTelegramId(long id);
        Task<User> AddUserAsync(string username, string firstName, string lastName, long telegramUserId);
    }
}
