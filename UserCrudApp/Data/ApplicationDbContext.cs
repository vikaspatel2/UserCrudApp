using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using UserCrudApp.Models;

namespace UserCrudApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Users> Users { get; set; }
    }
}
