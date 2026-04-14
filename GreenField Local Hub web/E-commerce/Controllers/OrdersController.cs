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
        public async Task<ActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            if (User.IsInRole("Admin"))
            {
                var allOrders = await _context.Order.Include(o => o.OrderProducts).ThenInclude(op => op.Product).ToListAsync();
                return View(allOrders);
            }
            else if (User.IsInRole("Producer"))
            {
                // Find all products belonging to this producer
                var producerProducts = await _context.Product
                    .Where(p => p.ProducerId != null && p.Producer.UserId == userId)
                    .Select(p => p.ProductId)
                    .ToListAsync();

                // Find orders that contain these products
                var producerOrders = await _context.OrderProduct.Where(op => producerProducts.Contains(op.ProductId)).Include(op => op.Order).ThenInclude(o => o.OrderProducts)
                    .ThenInclude(op => op.Product)
                    .Select(op => op.Order)
                    .Distinct()
                    .ToListAsync();

                return View(producerOrders);
            }
            else // Regular user/customer
            {
                var userOrders = await _context.Order.Where(o => o.UserId == userId).Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Product)
                    .ToListAsync();
                return View(userOrders);
            }
        }

        // Order Details action for all user types
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            IQueryable<OrderProduct> orderProductsQuery = _context.OrderProduct
                .Where(op => op.OrderId == id)
                .Include(op => op.Order)
                .Include(op => op.Product)
                .ThenInclude(p => p.Producer);

            // Apply different filters based on user role
            if (User.IsInRole("Admin"))
            {
                // Admins can see all order details
                // No additional filtering needed
            }
            else if (User.IsInRole("Producer"))
            {
                // Producers can only see their own products in the order
                orderProductsQuery = orderProductsQuery
                    .Where(op => op.Product.Producer.UserId == userId);
            }
            else // Regular user
            {
                // Regular users can only see their own orders
                orderProductsQuery = orderProductsQuery
                    .Where(op => op.Order.UserId == userId);
            }

            var orderProducts = await orderProductsQuery.ToListAsync();

            if (orderProducts == null || !orderProducts.Any())
            {
                return NotFound();
            }

            return View(orderProducts);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.OrderProduct
                .Where(op => op.OrderId == id)
                .Include(op => op.Product)
                .Include(op => op.Order)
                .ToListAsync();

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
        public async Task<IActionResult> Create(Order order, string orderMethod, int basketId)
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

            // Set OrderMethod from form
            order.OrderMethod = orderMethod;

            // Validate order method is selected
            if (string.IsNullOrEmpty(orderMethod))
            {
                ModelState.AddModelError("OrderMethod", "Must choose Collection or Delivery");
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
            if (orderMethod == "collection")
            {
                // Clear delivery fields
                order.DeliveryType = null;
                order.DeliveryAddress = null;
                order.ShippingFee = 0m;

                ModelState.Remove("DeliveryType");
                ModelState.Remove("DeliveryAddress");

                // Validate collection date
                if (order.ScheduleDate == null)
                {
                    ModelState.AddModelError("ScheduleDate", "Collection date is required.");
                }
                else
                {
                    var earliestDate = DateTime.Now.Date.AddDays(2);
                    if (order.ScheduleDate.Value.Date < earliestDate)
                    {
                        ModelState.AddModelError("ScheduleDate", "Collection must be at least 2 days from today.");
                    }
                }
            }
            else if (orderMethod == "delivery")
            {
                // Clear collection date
                order.ScheduleDate = null;
                ModelState.Remove("ScheduleDate");

                // Validate delivery type
                if (string.IsNullOrWhiteSpace(order.DeliveryType))
                {
                    ModelState.AddModelError("DeliveryType", "Delivery type is required.");
                }

                // Validate delivery address
                if (string.IsNullOrWhiteSpace(order.DeliveryAddress))
                {
                    ModelState.AddModelError("DeliveryAddress", "Delivery address is required.");
                }

                // Set shipping fee based on delivery type
                order.ShippingFee = order.DeliveryType switch
                {
                    "Next Day" => 9.99m,
                    "First Class" => 4.99m,
                    "Standard" => 2.99m,
                    _ => 0m
                };
            }
            else
            {
                ModelState.AddModelError("OrderMethod", "Invalid order method selected");
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

            // CHECK STOCK BEFORE CREATING ORDER
            foreach (var basketProduct in basketProducts)
            {
                if (basketProduct.Product.StockQuantity < basketProduct.Quantity)
                {
                    ModelState.AddModelError("", $"Not enough stock for {basketProduct.Product.ProductName}. Available: {basketProduct.Product.StockQuantity}");
                    ViewBag.BasketId = basketId;
                    return View(order);
                }
            }

            // Save order
            _context.Order.Add(order);
            await _context.SaveChangesAsync();

            // Save order items and update stock
            foreach (var basketProduct in basketProducts)
            {
                if (basketProduct.Product == null) continue;

                _context.OrderProduct.Add(new OrderProduct
                {
                    OrderId = order.OrderId,
                    ProductId = basketProduct.ProductId,
                    Quantity = basketProduct.Quantity,
                    UnitPrice = basketProduct.Product.Price
                });

                // UPDATE STOCK QUANTITY
                basketProduct.Product.StockQuantity -= basketProduct.Quantity;
            }

            // Close + clear basket
            basket.Status = false;
            _context.BasketProduct.RemoveRange(basketProducts);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
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
