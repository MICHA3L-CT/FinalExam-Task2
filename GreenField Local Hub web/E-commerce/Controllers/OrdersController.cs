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
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Order.Include(o => o.Discount);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order
                .Include(o => o.Discount)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create

        public IActionResult Create(int basketId)
        {
            ViewBag.BasketId = basketId;
            return View();
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order, int basketId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            // Validate basket ownership
            var basket = await _context.Basket
                .FirstOrDefaultAsync(b => b.BasketId == basketId && b.UserId == userId && b.Status);

            if (basket == null)
            {
                return NotFound();
            }

            // Get basket products safely
            var basketProducts = await _context.BasketProduct
                .Where(bp => bp.BasketId == basket.BasketId)
                .Include(bp => bp.Product)
                .ToListAsync();

            if (!basketProducts.Any())
            {
                ModelState.AddModelError("", "Your basket is empty.");
                ViewBag.BasketId = basketId;
                return View(order);
            }

            // Safe subtotal
            decimal subtotal = basketProducts
                .Where(bp => bp.Product != null)
                .Sum(bp => bp.Product.Price * bp.Quantity);

            // Loyalty discount
            var orderCount = await _context.Order.CountAsync(o => o.UserId == userId);
            decimal discount = orderCount >= 5 ? subtotal * 0.10m : 0m;

            // Order setup
            order.UserId = userId;
            order.OrderDate = DateTime.Now;
            order.OrderStatus = "Pending";

            // Delivery vs Collection
            if (order.OrderMethod == "collection")
            {
                order.DeliveryType = "Collection";
                order.ShippingFee = 0m;
                order.DeliveryAddress = null;

                ModelState.Remove("DeliveryAddress");
            }
            else
            {
                order.ShippingFee = order.DeliveryType switch
                {
                    "Next Day" => 9.99m,
                    "First Class" => 4.99m,
                    "Standard" => 2.99m,
                    _ => 0m
                };

                order.ScheduleDate = null;
                ModelState.Remove("ScheduleDate");
            }

            order.TotalAmount = (subtotal - discount) + order.ShippingFee;

            ModelState.Remove("UserId");
            ModelState.Remove("OrderStatus");
            ModelState.Remove("OrderDate");
            ModelState.Remove("TotalAmount");

            if (!ModelState.IsValid)
            {
                ViewBag.BasketId = basketId;
                return View(order);
            }

            // Save order
            _context.Order.Add(order);
            await _context.SaveChangesAsync();

            // Save order items
            foreach (var bp in basketProducts)
            {
                if (bp.Product == null) continue;

                _context.OrderProduct.Add(new OrderProduct
                {
                    OrderId = order.OrderId,
                    ProductId = bp.ProductId,
                    Quantity = bp.Quantity,
                    UnitPrice = bp.Product.Price
                });
            }

            // Close + clear basket
            basket.Status = false;
            _context.BasketProduct.RemoveRange(basketProducts);

            await _context.SaveChangesAsync();

            return RedirectToAction("Confirmation", new { id = order.OrderId });
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            ViewData["DiscountId"] = new SelectList(_context.Discount, "DiscountId", "DiscountId", order.DiscountId);
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderId,UserId,DiscountId,OrderDate,OrderStatus,TotalAmount,DeliveryAddress,ShippingFee,DeliveryType,ScheduleDate")] Order order)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.OrderId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DiscountId"] = new SelectList(_context.Discount, "DiscountId", "DiscountId", order.DiscountId);
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order
                .Include(o => o.Discount)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Order.FindAsync(id);
            if (order != null)
            {
                _context.Order.Remove(order);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Order.Any(e => e.OrderId == id);
        }
    }
}
