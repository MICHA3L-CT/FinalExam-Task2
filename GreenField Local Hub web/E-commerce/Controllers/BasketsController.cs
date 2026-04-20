using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using E_commerce.Data;
using E_commerce.Models;
using System.Security.Claims;

namespace E_commerce.Controllers
{
    public class BasketsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BasketsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Baskets/Count  – used by the cart badge (returns JSON)
        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Json(new { count = 0 });

            var basket = await _context.Basket
                .FirstOrDefaultAsync(b => b.UserId == userId && b.Status);

            if (basket == null)
                return Json(new { count = 0 });

            var count = await _context.BasketProduct
                .Where(bp => bp.BasketId == basket.BasketId)
                .SumAsync(bp => (int?)bp.Quantity) ?? 0;

            return Json(new { count });
        }

        // GET: Baskets
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Redirect("/Identity/Account/Login?ReturnUrl=%2FBaskets");

            var basket = await _context.Basket.FirstOrDefaultAsync(b => b.UserId == userId && b.Status);
            if (basket == null)
            {
                basket = new Basket
                {
                    UserId = userId,
                    Status = true,
                    CreatedDate = DateTime.Now
                };
                _context.Basket.Add(basket);
                await _context.SaveChangesAsync();
            }

            var basketProducts = await _context.BasketProduct
                .Where(bp => bp.BasketId == basket.BasketId)
                .Include(bp => bp.Product)
                .ToListAsync();

            decimal subtotal = basketProducts.Sum(bp => bp.Product.Price * bp.Quantity);

            var orderCount = await _context.Order.CountAsync(o => o.UserId == userId);

            decimal discount = 0m;
            if (orderCount >= 5)
                discount = subtotal * 0.10m;

            decimal deliveryFee = subtotal > 0 ? 3.99m : 0m;
            decimal total = subtotal - discount + deliveryFee;

            ViewBag.Subtotal = subtotal;
            ViewBag.Discount = discount;
            ViewBag.DeliveryFee = deliveryFee;
            ViewBag.Total = total;
            ViewBag.OrderCount = orderCount;
            ViewBag.BasketId = basket.BasketId;
            // Loyalty progress: orders toward 5-order threshold
            ViewBag.LoyaltyOrderCount = orderCount;

            return View(basketProducts);
        }

        // GET: Baskets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var basket = await _context.Basket.FirstOrDefaultAsync(m => m.BasketId == id);
            if (basket == null) return NotFound();

            return View(basket);
        }

        // GET: Baskets/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Baskets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BasketId,UserId,Status,CreatedDate")] Basket basket)
        {
            if (ModelState.IsValid)
            {
                _context.Add(basket);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(basket);
        }

        // GET: Baskets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var basket = await _context.Basket.FindAsync(id);
            if (basket == null) return NotFound();
            return View(basket);
        }

        // POST: Baskets/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BasketId,UserId,Status,CreatedDate")] Basket basket)
        {
            if (id != basket.BasketId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(basket);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BasketExists(basket.BasketId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(basket);
        }

        // GET: Baskets/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var basket = await _context.Basket.FirstOrDefaultAsync(m => m.BasketId == id);
            if (basket == null) return NotFound();

            return View(basket);
        }

        // POST: Baskets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var basket = await _context.Basket.FindAsync(id);
            if (basket != null)
                _context.Basket.Remove(basket);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BasketExists(int id)
        {
            return _context.Basket.Any(e => e.BasketId == id);
        }
    }
}
