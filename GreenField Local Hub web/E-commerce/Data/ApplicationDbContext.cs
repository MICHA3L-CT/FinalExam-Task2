using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using E_commerce.Models;

namespace E_commerce.Data
{
    // The main database context for the application.
    // Inherits from IdentityDbContext so it includes all the Identity tables
    // (users, roles, claims etc.) alongside our custom tables.
    public class ApplicationDbContext : IdentityDbContext
    {
        // Constructor receives database options (connection string etc.) from dependency injection
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Each DbSet maps to a table in the database.
        // EF Core uses these to query and save data for each model.
        public DbSet<E_commerce.Models.Basket> Basket { get; set; } = default!;
        public DbSet<E_commerce.Models.BasketProduct> BasketProduct { get; set; } = default!;
        public DbSet<E_commerce.Models.Discount> Discount { get; set; } = default!;
        public DbSet<E_commerce.Models.Order> Order { get; set; } = default!;
        public DbSet<E_commerce.Models.OrderProduct> OrderProduct { get; set; } = default!;
        public DbSet<E_commerce.Models.Producer> Producer { get; set; } = default!;
        public DbSet<E_commerce.Models.Product> Product { get; set; } = default!;
    }
}
