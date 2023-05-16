using Entity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Entity;

public class GpTalkDbContext:DbContext
{
    private readonly IConfiguration _config;

    public GpTalkDbContext(IConfiguration config)
    {
        _config = config;
    }

        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql(_config.GetValue<string>("ConnectionString"));

}