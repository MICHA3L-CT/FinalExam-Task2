using E_commerce.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E_commerce.Data
{
    public class SeedData
    {
        public static async Task SeedUsersAndRoles(IServiceProvider serviceProvider,UserManager<IdentityUser> userManager,RoleManager<IdentityRole> roleManager)
        {
            // Seed roles
            string[] roleNames = { "Admin", "Producer", "Customer", "Developer" };

            foreach (string roleName in roleNames)
            {
                var roleExists = await roleManager.RoleExistsAsync(roleName);

                if (!roleExists)
                {
                    var role = new IdentityRole(roleName);
                    await roleManager.CreateAsync(role);
                }
            }

            // Admin
            var adminUser = await userManager.FindByEmailAsync("admin@example.com");
            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = "admin@example.com",
                    Email = "admin@example.com",
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(adminUser, "Password123!");
            }

            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // Producers
            var producerUser = await userManager.FindByEmailAsync("producer@example.com");
            if (producerUser == null)
            {
                producerUser = new IdentityUser
                {
                    UserName = "producer@example.com",
                    Email = "producer@example.com",
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(producerUser, "Password123!");
            }

            if (!await userManager.IsInRoleAsync(producerUser, "Producer"))
            {
                await userManager.AddToRoleAsync(producerUser, "Producer");
            }

            var producerUser2 = await userManager.FindByEmailAsync("producer2@example.com");
            if (producerUser2 == null)
            {
                producerUser2 = new IdentityUser
                {
                    UserName = "producer2@example.com",
                    Email = "producer2@example.com",
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(producerUser2, "Password123!");
            }

            if (!await userManager.IsInRoleAsync(producerUser2, "Producer"))
            {
                await userManager.AddToRoleAsync(producerUser2, "Producer");
            }

            var producerUser3 = await userManager.FindByEmailAsync("producer3@example.com");
            if (producerUser3 == null)
            {
                producerUser3 = new IdentityUser
                {
                    UserName = "producer3@example.com",
                    Email = "producer3@example.com",
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(producerUser3, "Password123!");
            }

            if (!await userManager.IsInRoleAsync(producerUser3, "Producer"))
            {
                await userManager.AddToRoleAsync(producerUser3, "Producer");
            }

            // Developer
            var devUser = await userManager.FindByEmailAsync("dev@example.com");
            if (devUser == null)
            {
                devUser = new IdentityUser
                {
                    UserName = "dev@example.com",
                    Email = "dev@example.com",
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(devUser, "Password123!");
            }

            if (!await userManager.IsInRoleAsync(devUser, "Developer"))
            {
                await userManager.AddToRoleAsync(devUser, "Developer");
            }

            // Customer
            var normalUser = await userManager.FindByEmailAsync("user@example.com");
            if (normalUser == null)
            {
                normalUser = new IdentityUser
                {
                    UserName = "user@example.com",
                    Email = "user@example.com",
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(normalUser, "Password123!");
            }

            if (!await userManager.IsInRoleAsync(normalUser, "Customer"))
            {
                await userManager.AddToRoleAsync(normalUser, "Customer");
            }
        }

        public static async Task SeedProducers(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            var producerUser1 = await userManager.FindByEmailAsync("producer@example.com");
            var producerUser2 = await userManager.FindByEmailAsync("producer2@example.com");
            var producerUser3 = await userManager.FindByEmailAsync("producer3@example.com");

            if (producerUser1 == null || producerUser2 == null || producerUser3 == null)
            {
                throw new Exception("Producer users not found.");
            }

            if (context.Producer.Any())
                return;

            var producers = new List<Producer>
            {
                new Producer
                {
                    ProducerName = "Daniel's Farmfoods",
                    PhoneNumber = "07700900001",
                    ProductDescription = "We grow a wide variety of seasonal fruits and vegetables using traditional organic methods...",
                    Location = "Birmingham",
                    ProducerInfo = "Daniel's Farm has been family-owned and operated for over 30 years...",
                    DateJoined = new DateOnly(2023, 4, 10),
                    IsVerified = true,
                    UserId = producerUser1.Id
                },
                new Producer
                {
                    ProducerName = "Green Valley Gardens",
                    PhoneNumber = "07700900002",
                    ProductDescription = "Green Valley Gardens specialises in freshly cut herbs...",
                    Location = "Coventry",
                    ProducerInfo = "Green Valley Gardens was established in 2018...",
                    DateJoined = new DateOnly(2023, 7, 22),
                    IsVerified = true,
                    UserId = producerUser2.Id
                },
                new Producer
                {
                    ProducerName = "Sunny Fields Farm",
                    PhoneNumber = "07700900003",
                    ProductDescription = "Sunny Fields Farm produces free-range eggs...",
                    Location = "Wolverhampton",
                    ProducerInfo = "Sunny Fields Farm has been in the Holloway family...",
                    DateJoined = new DateOnly(2024, 1, 5),
                    IsVerified = true,
                    UserId = producerUser3.Id
                }
            };

            await context.Producer.AddRangeAsync(producers);
            await context.SaveChangesAsync();
        }

        public static async Task SeedProducts(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            if (context.Product.Any())
                return;

            var danielsFarmfoods = await context.Producer.FirstOrDefaultAsync(p => p.ProducerName == "Daniel's Farmfoods");
            var greenValleyGardens = await context.Producer.FirstOrDefaultAsync(p => p.ProducerName == "Green Valley Gardens");
            var sunnyFieldsFarm = await context.Producer.FirstOrDefaultAsync(p => p.ProducerName == "Sunny Fields Farm");

            if (danielsFarmfoods == null || greenValleyGardens == null || sunnyFieldsFarm == null)
                throw new Exception("Producer not found.");

            var products = new List<Product>
            {
                new Product
                {
                    ProductName = "Apples",
                    Description = "Fresh, crisp organic apples harvested at peak ripeness.",
                    Price = 1.50m,
                    StockQuantity = 100,
                    Category = "Fruit",
                    ImagePath = "/Images/apples.jpg",
                    ProducerId = danielsFarmfoods.ProducerId,
                    DateAdded = DateOnly.FromDateTime(DateTime.Now),
                },
                new Product
                {
                    ProductName = "Carrots",
                    Description = "Sweet and crunchy organic carrots.",
                    Price = 0.90m,
                    StockQuantity = 120,
                    Category = "Vegetables",
                    ImagePath = "/Images/carrots.jpg",
                    ProducerId = danielsFarmfoods.ProducerId,
                    DateAdded = DateOnly.FromDateTime(DateTime.Now),
                },
                new Product
                {
                    ProductName = "Cabbage",
                    Description = "Firm, leafy green cabbage.",
                    Price = 1.20m,
                    StockQuantity = 80,
                    Category = "Vegetables",
                    ImagePath = "/Images/Cabbage.jpg",
                    ProducerId = danielsFarmfoods.ProducerId,
                    DateAdded = DateOnly.FromDateTime(DateTime.Now),
                },
                new Product
                {
                    ProductName = "Beef (500g)",
                    Description = "High-quality, grass-fed beef.",
                    Price = 6.99m,
                    StockQuantity = 50,
                    Category = "Meat",
                    ImagePath = "/Images/Beef.jpg",
                    ProducerId = danielsFarmfoods.ProducerId,
                    DateAdded = DateOnly.FromDateTime(DateTime.Now),
                },
                new Product
                {
                    ProductName = "Spinach",
                    Description = "Fresh, nutrient-rich spinach leaves.",
                    Price = 1.30m,
                    StockQuantity = 90,
                    Category = "Vegetables",
                    ImagePath = "/Images/spinach.jpg",
                    ProducerId = greenValleyGardens.ProducerId,
                    DateAdded = DateOnly.FromDateTime(DateTime.Now),
                },
                new Product
                {
                    ProductName = "Strawberries",
                    Description = "Sweet and juicy strawberries.",
                    Price = 2.50m,
                    StockQuantity = 70,
                    Category = "Fruit",
                    ImagePath = "/Images/Strawberries.jpg",
                    ProducerId = greenValleyGardens.ProducerId,
                    DateAdded = DateOnly.FromDateTime(DateTime.Now),
                },
                new Product
                {
                    ProductName = "Free-Range Eggs (Dozen)",
                    Description = "Farm-fresh free-range eggs.",
                    Price = 3.20m,
                    StockQuantity = 60,
                    Category = "Dairy",
                    ImagePath = "/Images/eggs.jpg",
                    ProducerId = sunnyFieldsFarm.ProducerId,
                    DateAdded = DateOnly.FromDateTime(DateTime.Now),
                },
                new Product
                {
                    ProductName = "Bananas",
                    Description = "Naturally sweet bananas.",
                    Price = 1.10m,
                    StockQuantity = 110,
                    Category = "Fruit",
                    ImagePath = "/Images/bananas.jpg",
                    ProducerId = greenValleyGardens.ProducerId,
                    DateAdded = DateOnly.FromDateTime(DateTime.Now),
                }
            };

            await context.Product.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }
    }
}