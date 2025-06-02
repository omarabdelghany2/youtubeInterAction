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
        public DbSet<watcher> Watchers { get; set; }

    }
}