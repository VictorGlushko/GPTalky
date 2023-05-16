using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly GpTalkDbContext _context;
        public IMessageRepository Messages { get; }
        public IUserRepository Users { get; }

        public UnitOfWork(GpTalkDbContext context)
        {
            _context = context;
            Messages = new MessageRepository(_context);
            Users = new UserRepository(_context);
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
