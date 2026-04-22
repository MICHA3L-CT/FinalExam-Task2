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
    // Handles order listing, checkout creation, status updates and admin editing.
    // What each role sees:
    //   Admin    - all orders in the system
    //   Producer - only orders that contain their products
    //   Customer - only their own orders
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Orders
        // Returns a different set of orders depending on the user's role
        public async Task<ActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            if (User.IsInRole("Admin"))
            {
                // Admins see every order with its product lines
                var allOrders = await _context.Order.Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Product)
                    .ToListAsync();
                return View(allOrders);
            }
            else if (User.IsInRole("Producer"))
            {
                // Get the IDs of all products this producer sells
                var producerProducts = await _context.Product
                    .Where(p => p.ProducerId != null && p.Producer.UserId == userId)
                    .Select(p => p.ProductId)
                    .ToListAsync();

                // Find all orders that contain at least one of those products
                var producerOrders = await _context.OrderProduct
                    .Where(op => producerProducts.Contains(op.ProductId))
                    .Include(op => op.Order)
                    .ThenInclude(o => o.OrderProducts)
                    .ThenInclude(op => op.Product)
                    .Select(op => op.Order)
                    .Distinct()
                    .ToListAsync();

                return View(producerOrders);
            }
            else
            {
                // Regular customers see only their own orders
                var userOrders = await _context.Order
                    .Where(o => o.UserId == userId)
                    .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Product)
                    .ToListAsync();
                return View(userOrders);
            }
        }

        // GET: /Orders/Details/5
        // Shows the full order detail page. Each role sees a filtered view:
        //   Admin    - all products in the order
        //   Producer - only the lines for their own products
        //   Customer - only their own order (returns 404 for someone else's order ID)
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            // Start with all order lines for this order, including the related Order and Product details
            IQueryable<OrderProduct> orderProductsQuery = _context.OrderProduct
                .Where(op => op.OrderId == id)
                .Include(op => op.Order)
                .Include(op => op.Product)
                .ThenInclude(p => p.Producer);

            if (User.IsInRole("Admin"))
            {
                // Admins can see every line - no extra filter needed
            }
            else if (User.IsInRole("Producer"))
            {
                // Producers only see the lines for products they sell
                orderProductsQuery = orderProductsQuery
                    .Where(op => op.Product.Producer.UserId == userId);
            }
            else
            {
                // Customers can only see lines from their own orders
                orderProductsQuery = orderProductsQuery
                    .Where(op => op.Order.UserId == userId);
            }

            var orderProducts = await orderProductsQuery.ToListAsync();

            if (orderProducts == null || !orderProducts.Any())
                return NotFound();

            return View(orderProducts);
        }

        // GET: /Orders/Create?basketId=3
        // Displays the checkout form, passing the basket ID through to the view
        public IActionResult Create(int basketId)
        {
            ViewBag.BasketId = basketId;
            return View();
        }

        // POST: /Orders/Create
        // Processes the checkout form, validates the order, deducts stock,
        // closes the basket and creates the order and order line records.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order, string orderMethod, int basketId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            // Make sure the basket exists and belongs to the logged-in user
            var basket = await _context.Basket
                .FirstOrDefaultAsync(b => b.BasketId == basketId && b.UserId == userId && b.Status);

            if (basket == null)
                return NotFound();

            // Load all basket lines with product prices
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

            order.OrderMethod = orderMethod;

            if (string.IsNullOrEmpty(orderMethod))
                ModelState.AddModelError("OrderMethod", "Must choose Collection or Delivery");

            // Calculate the basket subtotal (null-safe filter in case any product was deleted)
            decimal subtotal = basketProducts
                .Where(bp => bp.Product != null)
                .Sum(bp => bp.Product.Price * bp.Quantity);

            // Apply loyalty discount if the user has completed a multiple of 5 orders
            var orderCount     = await _context.Order.CountAsync(o => o.UserId == userId);
            bool discountEarned = orderCount > 0 && orderCount % 5 == 0;
            decimal discount   = discountEarned ? subtotal * 0.10m : 0m;

            // Set system-controlled fields that should not come from the form
            order.UserId      = userId;
            order.OrderDate   = DateTime.Now;
            order.OrderStatus = "Pending";

            if (orderMethod == "collection")
            {
                // Collection orders have no delivery address or shipping fee
                order.DeliveryType    = null;
                order.DeliveryAddress = null;
                order.ShippingFee     = 0m;

                ModelState.Remove("DeliveryType");
                ModelState.Remove("DeliveryAddress");

                if (order.ScheduleDate == null)
                {
                    ModelState.AddModelError("ScheduleDate", "Collection date is required.");
                }
                else
                {
                    // Collection must be booked at least 2 days in advance
                    var earliestDate = DateTime.Now.Date.AddDays(2);
                    if (order.ScheduleDate.Value.Date < earliestDate)
                        ModelState.AddModelError("ScheduleDate", "Collection must be at least 2 days from today.");
                }
            }
            else if (orderMethod == "delivery")
            {
                // Delivery orders do not use a schedule date
                order.ScheduleDate = null;
                ModelState.Remove("ScheduleDate");

                if (string.IsNullOrWhiteSpace(order.DeliveryType))
                    ModelState.AddModelError("DeliveryType", "Delivery type is required.");

                if (string.IsNullOrWhiteSpace(order.DeliveryAddress))
                    ModelState.AddModelError("DeliveryAddress", "Delivery address is required.");

                // Set shipping fee based on the chosen delivery speed
                order.ShippingFee = order.DeliveryType switch
                {
                    "Next Day"    => 9.99m,
                    "First Class" => 4.99m,
                    "Standard"    => 2.99m,
                    _             => 0m
                };
            }
            else
            {
                ModelState.AddModelError("OrderMethod", "Invalid order method selected");
            }

            order.TotalAmount = (subtotal - discount) + order.ShippingFee;

            // Remove validation for fields we set manually so they do not block the form
            ModelState.Remove("UserId");
            ModelState.Remove("OrderStatus");
            ModelState.Remove("OrderDate");
            ModelState.Remove("TotalAmount");

            if (!ModelState.IsValid)
            {
                ViewBag.BasketId = basketId;
                return View(order);
            }

            // Check that every product has enough stock before committing anything
            foreach (var basketProduct in basketProducts)
            {
                if (basketProduct.Product.StockQuantity < basketProduct.Quantity)
                {
                    ModelState.AddModelError("", $"Not enough stock for {basketProduct.Product.ProductName}. Available: {basketProduct.Product.StockQuantity}");
                    ViewBag.BasketId = basketId;
                    return View(order);
                }
            }

            // Save the order header first so we get an OrderId for the lines
            _context.Order.Add(order);
            await _context.SaveChangesAsync();

            // Create an OrderProduct line for each basket item and reduce the stock level
            foreach (var basketProduct in basketProducts)
            {
                if (basketProduct.Product == null) continue;

                _context.OrderProduct.Add(new OrderProduct
                {
                    OrderId   = order.OrderId,
                    ProductId = basketProduct.ProductId,
                    Quantity  = basketProduct.Quantity,
                    UnitPrice = basketProduct.Product.Price  // Snapshot of price at time of purchase
                });

                // Deduct the purchased quantity from the live stock count
                basketProduct.Product.StockQuantity -= basketProduct.Quantity;
            }

            // Mark the basket as closed and remove all its product lines
            basket.Status = false;
            _context.BasketProduct.RemoveRange(basketProducts);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // GET: /Orders/Edit/5
        // Admin-only form to edit any field on an order
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var order = await _context.Order.FindAsync(id);
            if (order == null)
                return NotFound();

            ViewData["DiscountId"] = new SelectList(_context.Discount, "DiscountId", "DiscountId", order.DiscountId);
            return View(order);
        }

        // POST: /Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderId,UserId,DiscountId,OrderDate,OrderStatus,TotalAmount,DeliveryAddress,ShippingFee,DeliveryType,ScheduleDate")] Order order)
        {
            if (id != order.OrderId)
                return NotFound();

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
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DiscountId"] = new SelectList(_context.Discount, "DiscountId", "DiscountId", order.DiscountId);
            return View(order);
        }

        // GET: /Orders/UpdateStatus/5
        // Allows Admin or Producer to change an order's status.
        // Producers can only update orders that contain their own products.
        public async Task<IActionResult> UpdateStatus(int? id)
        {
            if (id == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            if (!User.IsInRole("Admin") && !User.IsInRole("Producer"))
                return Forbid();

            var order = await _context.Order.FindAsync(id);
            if (order == null) return NotFound();

            // If the user is a producer (but not also an admin), verify they own a product in this order
            if (User.IsInRole("Producer") && !User.IsInRole("Admin"))
            {
                var producerProductIds = await _context.Product
                    .Where(p => p.Producer.UserId == userId)
                    .Select(p => p.ProductId)
                    .ToListAsync();

                bool ownsProduct = await _context.OrderProduct
                    .AnyAsync(op => op.OrderId == id && producerProductIds.Contains(op.ProductId));

                if (!ownsProduct) return Forbid();
            }

            return View(order);
        }

        // POST: /Orders/UpdateStatus/5
        // Only updates the OrderStatus field - does not touch any other order data.
        // This is intentional so producers cannot change prices or addresses.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string orderStatus)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            if (!User.IsInRole("Admin") && !User.IsInRole("Producer"))
                return Forbid();

            var order = await _context.Order.FindAsync(id);
            if (order == null) return NotFound();

            // Ownership check for producers
            if (User.IsInRole("Producer") && !User.IsInRole("Admin"))
            {
                var producerProductIds = await _context.Product
                    .Where(p => p.Producer.UserId == userId)
                    .Select(p => p.ProductId)
                    .ToListAsync();

                bool ownsProduct = await _context.OrderProduct
                    .AnyAsync(op => op.OrderId == id && producerProductIds.Contains(op.ProductId));

                if (!ownsProduct) return Forbid();
            }

            // Only update the status - nothing else changes
            order.OrderStatus = orderStatus;
            _context.Update(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var order = await _context.Order
                .Include(o => o.Discount)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
                return NotFound();

            return View(order);
        }

        // POST: /Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Order.FindAsync(id);
            if (order != null)
                _context.Order.Remove(order);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Helper - checks whether an order with the given ID exists
        private bool OrderExists(int id)
        {
            return _context.Order.Any(e => e.OrderId == id);
        }
    }
}
