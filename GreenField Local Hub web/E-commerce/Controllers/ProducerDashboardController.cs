using E_commerce.Data;
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
            {
                return NotFound();
            }

            var products = await _context.Product
                .Where(p => p.ProducerId == producer.ProducerId)
                .ToListAsync();

            var orders = await _context.Order.Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Product)
                .Where(o => o.OrderProducts
                    .Any(op => op.Product != null && op.Product.ProducerId == producer.ProducerId))
                .ToListAsync();

            ViewBag.Orders = orders;
            ViewBag.Producer = producer;

            return View(products);
        }

    }
}
