using E_commerce.Data;
using E_commerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace E_commerce.Controllers
{
    [Authorize(Roles = "Producer")]
    public class ProducerDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProducerDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var producer = await _context.Producer
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (producer == null)
                return NotFound();

            var products = await _context.Product
                .Where(p => p.ProducerId == producer.ProducerId)
                .ToListAsync();

            var orders = await _context.Order.Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Product)
                .Where(o => o.OrderProducts
                    .Any(op => op.Product != null && op.Product.ProducerId == producer.ProducerId))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Calculate total revenue from all orders containing this producer's products
            decimal totalRevenue = orders
                .SelectMany(o => o.OrderProducts)
                .Where(op => op.Product != null && op.Product.ProducerId == producer.ProducerId)
                .Sum(op => op.Product.Price * op.Quantity);

            ViewBag.TotalProducts = products.Count;
            ViewBag.LowStockCount = products.Count(p => p.StockQuantity < 5);
            ViewBag.TotalStock = products.Sum(p => p.StockQuantity);
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.RecentOrders = orders;
            ViewBag.ProducerName = producer.ProducerName ?? "Producer";

            return View(products);
        }
    }
}
