using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.Repositories
{
    public interface IUnitOfWork
    {
        IMessageRepository Messages { get; }
        IUserRepository Users { get; }
        Task<int> CompleteAsync();
    }
}
