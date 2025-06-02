using Microsoft.EntityFrameworkCore;
using SignalRGame.Models;




namespace SignalRGame.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<MyUser> Users { get; set; }
        public DbSet<Watcher> Watchers { get; set; }

        public DbSet<StreamlabsToken> StreamlabsTokens { get; set; }


    }
}