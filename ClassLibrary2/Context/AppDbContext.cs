using Entities.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace DataAccess.Context
{
  public class AppDbContext:IdentityDbContext<User,Role,Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options):base(options)
        {
            
        }
        //public int MyProperty { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<User>().ToTable("Users");

            builder.Entity<Role>().ToTable("Roles");
        }

    }
}
