using Entity;
using Entity.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Core
{
    public class Commander
    {
        private readonly IUnitOfWork _dbContext;

        public Commander(IUnitOfWork dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task FigureOutAsync(string command, User user)
        {
            switch (command)
            {
                case Commands.Start: await Start(user); break;
                case Commands.Clear: await ClearHistory(user.Id); break;
            }
        }

        private async Task Start(User user)
        {
            var userFromDb = await _dbContext.Users.GetUserByTelegramId(user.Id);

            if (userFromDb is null)
            { 
                await _dbContext.Users.AddUserAsync(user.Username, user.FirstName, user.LastName, user.Id);
                await _dbContext.CompleteAsync();
            }
        }

        private async Task ClearHistory(long telegramUserId)
        {
            await _dbContext.Messages.ClearAllMessagesAsync(telegramUserId);
            await _dbContext.CompleteAsync();
        }
    }
}
