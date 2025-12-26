using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using UserCrudApp.Models;

namespace UserCrudApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Users>()
                .ToTable("tbl_Users", "dbo");
        }

        public DbSet<Users> Users { get; set; }

        public DbSet<ApiLog> ApiLog { get; set; }
    }
}
