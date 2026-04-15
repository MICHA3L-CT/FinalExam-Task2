using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using E_commerce.Models;

namespace E_commerce.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<E_commerce.Models.Basket> Basket { get; set; } = default!;
        public DbSet<E_commerce.Models.BasketProduct> BasketProduct { get; set; } = default!;
        public DbSet<E_commerce.Models.Discount> Discount { get; set; } = default!;
        public DbSet<E_commerce.Models.Order> Order { get; set; } = default!;
        public DbSet<E_commerce.Models.OrderProduct> OrderProduct { get; set; } = default!;
        public DbSet<E_commerce.Models.Producer> Producer { get; set; } = default!;
        public DbSet<E_commerce.Models.Product> Product { get; set; } = default!;
    }
}
