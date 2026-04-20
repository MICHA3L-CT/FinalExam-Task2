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
    public class BasketProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BasketProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: BasketProducts
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.BasketProduct.Include(b => b.Basket).Include(b => b.Product);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: BasketProducts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var basketProduct = await _context.BasketProduct
                .Include(b => b.Basket)
                .Include(b => b.Product)
                .FirstOrDefaultAsync(m => m.BasketProductId == id);

            if (basketProduct == null) return NotFound();
            return View(basketProduct);
        }

        // POST: BasketProducts/Create  (AJAX – returns JSON, no redirect)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int productId)
        {
            var product = await _context.Product.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null)
                return Json(new { success = false, message = "Product not found." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Json(new { success = false, message = "Please log in to add items to your basket." });

            var basket = await _context.Basket.FirstOrDefaultAsync(b => b.UserId == userId && b.Status == true);
            if (basket == null)
            {
                basket = new Basket
                {
                    UserId = userId,
                    Status = true,
                    CreatedDate = DateTime.UtcNow
                };
                _context.Basket.Add(basket);
                await _context.SaveChangesAsync();
            }

            var basketProduct = await _context.BasketProduct
                .FirstOrDefaultAsync(bp => bp.BasketId == basket.BasketId && bp.ProductId == productId);

            if (basketProduct != null)
            {
                basketProduct.Quantity++;
                basketProduct.TotalPrice = basketProduct.Quantity * product.Price;
                _context.BasketProduct.Update(basketProduct);
            }
            else
            {
                basketProduct = new BasketProduct
                {
                    ProductId = productId,
                    BasketId = basket.BasketId,
                    Quantity = 1,
                    TotalPrice = product.Price
                };
                _context.BasketProduct.Add(basketProduct);
            }

            await _context.SaveChangesAsync();

            // Return the new basket item count for the cart badge
            var count = await _context.BasketProduct
                .Where(bp => bp.BasketId == basket.BasketId)
                .SumAsync(bp => bp.Quantity);

            return Json(new
            {
                success = true,
                message = $"{product.ProductName} added to basket.",
                cartCount = count
            });
        }

        // GET: BasketProducts/Create
        public IActionResult Create()
        {
            ViewData["BasketId"] = new SelectList(_context.Basket, "BasketId", "BasketId");
            ViewData["ProductId"] = new SelectList(_context.Set<Product>(), "ProductId", "ProductId");
            return View();
        }

        // GET: BasketProducts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var basketProduct = await _context.BasketProduct.FindAsync(id);
            if (basketProduct == null) return NotFound();

            ViewData["BasketId"] = new SelectList(_context.Basket, "BasketId", "BasketId", basketProduct.BasketId);
            ViewData["ProductId"] = new SelectList(_context.Set<Product>(), "ProductId", "ProductId", basketProduct.ProductId);
            return View(basketProduct);
        }

        // POST: BasketProducts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BasketProductId,ProductId,BasketId,Quantity,TotalPrice")] BasketProduct basketProduct)
        {
            if (id != basketProduct.BasketProductId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(basketProduct);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BasketProductExists(basketProduct.BasketProductId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["BasketId"] = new SelectList(_context.Basket, "BasketId", "BasketId", basketProduct.BasketId);
            ViewData["ProductId"] = new SelectList(_context.Set<Product>(), "ProductId", "ProductId", basketProduct.ProductId);
            return View(basketProduct);
        }

        // GET: BasketProducts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var basketProduct = await _context.BasketProduct
                .Include(b => b.Basket)
                .Include(b => b.Product)
                .FirstOrDefaultAsync(m => m.BasketProductId == id);

            if (basketProduct == null) return NotFound();
            return View(basketProduct);
        }

        // POST: BasketProducts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var basketProduct = await _context.BasketProduct.FindAsync(id);
            if (basketProduct != null)
                _context.BasketProduct.Remove(basketProduct);

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Baskets");
        }

        // POST: BasketProducts/IncreaseQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncreaseQuantity(int id)
        {
            var basketProduct = await _context.BasketProduct
                .Include(bp => bp.Product)
                .FirstOrDefaultAsync(bp => bp.BasketProductId == id);

            if (basketProduct == null)
                return NotFound();

            basketProduct.Quantity++;
            basketProduct.TotalPrice = basketProduct.Quantity * basketProduct.Product.Price;
            _context.BasketProduct.Update(basketProduct);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Baskets");
        }

        // POST: BasketProducts/DecreaseQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecreaseQuantity(int id)
        {
            var basketProduct = await _context.BasketProduct
                .Include(bp => bp.Product)
                .FirstOrDefaultAsync(bp => bp.BasketProductId == id);

            if (basketProduct == null)
                return NotFound();

            if (basketProduct.Quantity > 1)
            {
                basketProduct.Quantity--;
                basketProduct.TotalPrice = basketProduct.Quantity * basketProduct.Product.Price;
                _context.BasketProduct.Update(basketProduct);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Remove item if quantity would go to 0
                _context.BasketProduct.Remove(basketProduct);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Baskets");
        }

        // POST: BasketProducts/Reorder  – re-adds all items from a past order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var orderProducts = await _context.OrderProduct
                .Include(op => op.Product)
                .Where(op => op.OrderId == orderId)
                .ToListAsync();

            if (!orderProducts.Any())
                return RedirectToAction("Index", "Orders");

            // Get or create active basket
            var basket = await _context.Basket
                .FirstOrDefaultAsync(b => b.UserId == userId && b.Status == true);

            if (basket == null)
            {
                basket = new Basket
                {
                    UserId = userId,
                    Status = true,
                    CreatedDate = DateTime.UtcNow
                };
                _context.Basket.Add(basket);
                await _context.SaveChangesAsync();
            }

            foreach (var op in orderProducts)
            {
                if (op.Product == null) continue;

                var existing = await _context.BasketProduct
                    .FirstOrDefaultAsync(bp => bp.BasketId == basket.BasketId && bp.ProductId == op.ProductId);

                if (existing != null)
                {
                    existing.Quantity += op.Quantity;
                    existing.TotalPrice = existing.Quantity * op.Product.Price;
                    _context.BasketProduct.Update(existing);
                }
                else
                {
                    _context.BasketProduct.Add(new BasketProduct
                    {
                        BasketId = basket.BasketId,
                        ProductId = op.ProductId,
                        Quantity = op.Quantity,
                        TotalPrice = op.Quantity * op.Product.Price
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Baskets");
        }

        private bool BasketProductExists(int id)
        {
            return _context.BasketProduct.Any(e => e.BasketProductId == id);
        }
    }
}
