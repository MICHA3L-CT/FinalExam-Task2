using E_commerce.Data;
using E_commerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace E_commerce.Controllers
{
    // The producer-only dashboard controller.
    // Restricted to users in the Producer role - any other user gets a 403 Forbidden.
    [Authorize(Roles = "Producer")]
    public class ProducerDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProducerDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /ProducerDashboard
        // Loads summary statistics and recent orders for the logged-in producer.
        // Passes data to the view via ViewBag because the model is the product list.
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Find the producer profile linked to the logged-in user account
            var producer = await _context.Producer
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (producer == null)
                return NotFound();

            // Get all products belonging to this producer
            var products = await _context.Product
                .Where(p => p.ProducerId == producer.ProducerId)
                .ToListAsync();

            // Get all orders that contain at least one of this producer's products,
            // ordered newest first for the recent orders table
            var orders = await _context.Order
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Product)
                .Where(o => o.OrderProducts
                    .Any(op => op.Product != null && op.Product.ProducerId == producer.ProducerId))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Calculate total revenue by summing price x quantity for this producer's items across all orders
            decimal totalRevenue = orders
                .SelectMany(o => o.OrderProducts)
                .Where(op => op.Product != null && op.Product.ProducerId == producer.ProducerId)
                .Sum(op => op.Product.Price * op.Quantity);

            // Pass all dashboard metrics to the view via ViewBag
            ViewBag.TotalProducts = products.Count;
            ViewBag.LowStockCount = products.Count(p => p.StockQuantity < 5);  // Products with fewer than 5 units left
            ViewBag.TotalStock    = products.Sum(p => p.StockQuantity);
            ViewBag.TotalRevenue  = totalRevenue;
            ViewBag.RecentOrders  = orders;
            ViewBag.ProducerName  = producer.ProducerName ?? "Producer";

            // The view model is the product list (used for the "Your Products" table)
            return View(products);
        }
    }
}
