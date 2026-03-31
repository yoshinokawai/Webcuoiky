using Microsoft.EntityFrameworkCore;
using WebWikiForum.Models;

namespace WebWikiForum.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Vtuber> Vtubers { get; set; }
        public DbSet<Agency> Agencies { get; set; }
    }
}
