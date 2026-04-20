using E_commerce.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E_commerce.Data
{
    public class SeedData
    {
        public static async Task SeedUsersAndRoles(
            IServiceProvider serviceProvider,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // ── Roles ────────────────────────────────────────────
            string[] roleNames = { "Admin", "Producer", "Customer", "Developer" };

            foreach (string roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // ── Helper: create or fix a user ────────────────────
            static async Task<IdentityUser> EnsureUser(
                UserManager<IdentityUser> um,
                string email,
                string password)
            {
                var user = await um.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new IdentityUser
                    {
                        UserName       = email,
                        Email          = email,
                        EmailConfirmed = true   // Always confirmed so login works without email flow
                    };
                    var result = await um.CreateAsync(user, password);
                    if (!result.Succeeded)
                        throw new Exception($"Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
                else if (!user.EmailConfirmed)
                {
                    // Fix any existing user whose email was never confirmed
                    user.EmailConfirmed = true;
                    await um.UpdateAsync(user);
                }
                return user;
            }

            static async Task EnsureRole(UserManager<IdentityUser> um, IdentityUser user, string role)
            {
                if (!await um.IsInRoleAsync(user, role))
                    await um.AddToRoleAsync(user, role);
            }

            // ── Seed users ───────────────────────────────────────
            var admin     = await EnsureUser(userManager, "admin@example.com",     "Password123!");
            var producer1 = await EnsureUser(userManager, "producer@example.com",  "Password123!");
            var producer2 = await EnsureUser(userManager, "producer2@example.com", "Password123!");
            var producer3 = await EnsureUser(userManager, "producer3@example.com", "Password123!");
            var dev       = await EnsureUser(userManager, "dev@example.com",        "Password123!");
            var customer  = await EnsureUser(userManager, "user@example.com",       "Password123!");

            // ── Assign roles ─────────────────────────────────────
            await EnsureRole(userManager, admin,     "Admin");
            await EnsureRole(userManager, producer1, "Producer");
            await EnsureRole(userManager, producer2, "Producer");
            await EnsureRole(userManager, producer3, "Producer");
            await EnsureRole(userManager, dev,       "Developer");
            await EnsureRole(userManager, customer,  "Customer");
        }

        public static async Task SeedProducers(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var context     = serviceProvider.GetRequiredService<ApplicationDbContext>();

            var producerUser1 = await userManager.FindByEmailAsync("producer@example.com");
            var producerUser2 = await userManager.FindByEmailAsync("producer2@example.com");
            var producerUser3 = await userManager.FindByEmailAsync("producer3@example.com");

            if (producerUser1 == null || producerUser2 == null || producerUser3 == null)
                throw new Exception("Producer users not found. Ensure SeedUsersAndRoles ran first.");

            if (context.Producer.Any())
                return;

            var producers = new List<Producer>
            {
                new Producer
                {
                    ProducerName       = "Daniel's Farmfoods",
                    PhoneNumber        = "07700900001",
                    ProductDescription = "We grow a wide variety of seasonal fruits and vegetables using traditional organic methods...",
                    Location           = "Birmingham",
                    ProducerInfo       = "Daniel's Farm has been family-owned and operated for over 30 years...",
                    DateJoined         = new DateOnly(2023, 4, 10),
                    IsVerified         = true,
                    UserId             = producerUser1.Id
                },
                new Producer
                {
                    ProducerName       = "Green Valley Gardens",
                    PhoneNumber        = "07700900002",
                    ProductDescription = "Green Valley Gardens specialises in freshly cut herbs...",
                    Location           = "Coventry",
                    ProducerInfo       = "Green Valley Gardens was established in 2018...",
                    DateJoined         = new DateOnly(2023, 7, 22),
                    IsVerified         = true,
                    UserId             = producerUser2.Id
                },
                new Producer
                {
                    ProducerName       = "Sunny Fields Farm",
                    PhoneNumber        = "07700900003",
                    ProductDescription = "Sunny Fields Farm produces free-range eggs...",
                    Location           = "Wolverhampton",
                    ProducerInfo       = "Sunny Fields Farm has been in the Holloway family...",
                    DateJoined         = new DateOnly(2024, 1, 5),
                    IsVerified         = true,
                    UserId             = producerUser3.Id
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

            var danielsFarmfoods   = await context.Producer.FirstOrDefaultAsync(p => p.ProducerName == "Daniel's Farmfoods");
            var greenValleyGardens = await context.Producer.FirstOrDefaultAsync(p => p.ProducerName == "Green Valley Gardens");
            var sunnyFieldsFarm    = await context.Producer.FirstOrDefaultAsync(p => p.ProducerName == "Sunny Fields Farm");

            if (danielsFarmfoods == null || greenValleyGardens == null || sunnyFieldsFarm == null)
                throw new Exception("Producers not found. Ensure SeedProducers ran first.");

            var products = new List<Product>
            {
                new Product
                {
                    ProductName   = "Apples",
                    Description   = "Fresh, crisp organic apples harvested at peak ripeness.",
                    Price         = 1.50m,
                    StockQuantity = 100,
                    Category      = "Fruit",
                    ImagePath     = "/Images/apples.jpg",
                    ProducerId    = danielsFarmfoods.ProducerId,
                    DateAdded     = DateOnly.FromDateTime(DateTime.Now),
                },
                new Product
                {
                    ProductName   = "Carrots",
                    Description   = "Sweet and crunchy organic carrots.",
                    Price         = 0.90m,
                    StockQuantity = 120,
                    Category      = "Vegetables",
                    ImagePath     = "/Images/carrots.jpg",
                    ProducerId    = danielsFarmfoods.ProducerId,
                    DateAdded     = DateOnly.FromDateTime(DateTime.Now),
                },
                new Product
                {
                    ProductName   = "Cabbage",
                    Description   = "Firm, leafy green cabbage.",
                    Price         = 1.20m,
                    StockQuantity = 80,
                    Category      = "Vegetables",
                    ImagePath     = "/Images/Cabbage.jpg",
                    ProducerId    = danielsFarmfoods.ProducerId,
                    DateAdded     = DateOnly.FromDateTime(DateTime.Now),
                },
                new Product
                {
                    ProductName   = "Beef (500g)",
                    Description   = "High-quality, grass-fed beef.",
                    Price         = 6.99m,
                    StockQuantity = 50,
                    Category      = "Meat",
                    ImagePath     = "/Images/Beef.jpg",
                    ProducerId    = danielsFarmfoods.ProducerId,
                    DateAdded     = DateOnly.FromDateTime(DateTime.Now),
                },
                new Product
                {
                    ProductName   = "Spinach",
                    Description   = "Fresh, nutrient-rich spinach leaves.",
                    Price         = 1.30m,
                    StockQuantity = 90,
                    Category      = "Vegetables",
                    ImagePath     = "/Images/spinach.jpg",
                    ProducerId    = greenValleyGardens.ProducerId,
                    DateAdded     = DateOnly.FromDateTime(DateTime.Now),
                },
                new Product
                {
                    ProductName   = "Strawberries",
                    Description   = "Sweet and juicy strawberries.",
                    Price         = 2.50m,
                    StockQuantity = 70,
                    Category      = "Fruit",
                    ImagePath     = "/Images/Strawberries.jpg",
                    ProducerId    = greenValleyGardens.ProducerId,
                    DateAdded     = DateOnly.FromDateTime(DateTime.Now),
                },
                new Product
                {
                    ProductName   = "Free-Range Eggs (Dozen)",
                    Description   = "Farm-fresh free-range eggs.",
                    Price         = 3.20m,
                    StockQuantity = 60,
                    Category      = "Dairy",
                    ImagePath     = "/Images/eggs.jpg",
                    ProducerId    = sunnyFieldsFarm.ProducerId,
                    DateAdded     = DateOnly.FromDateTime(DateTime.Now),
                },
                new Product
                {
                    ProductName   = "Bananas",
                    Description   = "Naturally sweet bananas.",
                    Price         = 1.10m,
                    StockQuantity = 110,
                    Category      = "Fruit",
                    ImagePath     = "/Images/bananas.jpg",
                    ProducerId    = greenValleyGardens.ProducerId,
                    DateAdded     = DateOnly.FromDateTime(DateTime.Now),
                }
            };

            await context.Product.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }
    }
}
