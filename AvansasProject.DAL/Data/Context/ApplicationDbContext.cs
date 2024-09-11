using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AvansasProject.MODEL.Models;

namespace AvansasProject.DAL.Data.Context
{
    public class ApplicationDbContext : IdentityDbContext
        
    {
        
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<Category> Categories { get; set; } 
        public DbSet<Product> Products { get; set; }

        
    }
}
