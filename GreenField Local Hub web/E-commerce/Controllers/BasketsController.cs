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
    // Handles viewing and managing the current user's shopping basket.
    // Also exposes a Count endpoint used by the cart badge in the navbar.
    public class BasketsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BasketsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Baskets/Count
        // Returns the total quantity of items in the user's active basket as JSON.
        // Called by JavaScript in the layout to keep the cart badge up to date.
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

            // Sum all quantities across every product line in the basket
            var count = await _context.BasketProduct
                .Where(bp => bp.BasketId == basket.BasketId)
                .SumAsync(bp => (int?)bp.Quantity) ?? 0;

            return Json(new { count });
        }

        // GET: /Baskets
        // Shows the user's current basket contents with subtotal, loyalty discount and total.
        // Creates a new empty basket automatically if the user does not have one yet.
        // Redirects to the login page if the user is not authenticated.
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Redirect("/Identity/Account/Login?ReturnUrl=%2FBaskets");

            // Find the user's active basket, or create one if it does not exist
            var basket = await _context.Basket.FirstOrDefaultAsync(b => b.UserId == userId && b.Status);
            if (basket == null)
            {
                basket = new Basket
                {
                    UserId      = userId,
                    Status      = true,
                    CreatedDate = DateTime.Now
                };
                _context.Basket.Add(basket);
                await _context.SaveChangesAsync();
            }

            // Load all basket lines with their associated product details
            var basketProducts = await _context.BasketProduct
                .Where(bp => bp.BasketId == basket.BasketId)
                .Include(bp => bp.Product)
                .ToListAsync();

            // Calculate the basket subtotal
            decimal subtotal = basketProducts.Sum(bp => bp.Product.Price * bp.Quantity);

            // Work out loyalty progress - orderCount % 5 gives position in the current cycle (0-4)
            var orderCount    = await _context.Order.CountAsync(o => o.UserId == userId);
            int loyaltyProgress = orderCount % 5;
            // Discount is earned when the user has completed a multiple of 5 orders
            bool discountEarned = orderCount > 0 && orderCount % 5 == 0;

            decimal discount = discountEarned ? subtotal * 0.10m : 0m;
            decimal total    = subtotal - discount;

            // Pass all calculated values to the view via ViewBag
            ViewBag.Subtotal         = subtotal;
            ViewBag.Discount         = discount;
            ViewBag.Total            = total;
            ViewBag.OrderCount       = orderCount;
            ViewBag.BasketId         = basket.BasketId;
            ViewBag.LoyaltyOrderCount = loyaltyProgress;

            return View(basketProducts);
        }

        // GET: /Baskets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var basket = await _context.Basket.FirstOrDefaultAsync(m => m.BasketId == id);
            if (basket == null) return NotFound();

            return View(basket);
        }

        // GET: /Baskets/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Baskets/Create
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

        // GET: /Baskets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var basket = await _context.Basket.FindAsync(id);
            if (basket == null) return NotFound();
            return View(basket);
        }

        // POST: /Baskets/Edit/5
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
                    // If the basket no longer exists in the database, return 404
                    if (!BasketExists(basket.BasketId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(basket);
        }

        // GET: /Baskets/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var basket = await _context.Basket.FirstOrDefaultAsync(m => m.BasketId == id);
            if (basket == null) return NotFound();

            return View(basket);
        }

        // POST: /Baskets/Delete/5
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

        // Helper - checks whether a basket with the given ID exists in the database
        private bool BasketExists(int id)
        {
            return _context.Basket.Any(e => e.BasketId == id);
        }
    }
}
