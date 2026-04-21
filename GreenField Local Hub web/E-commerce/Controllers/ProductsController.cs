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
using Microsoft.AspNetCore.Authorization;

namespace E_commerce.Controllers
{
    // Handles listing, creating, editing and deleting products.
    // Customers see all products (or filtered by producer).
    // Producers only see and manage their own products.
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Products or /Products?producerId=3
        // If the logged-in user is a Producer, only their own products are shown.
        // If a producerId query parameter is supplied, filters to that producer's products.
        // Otherwise all products are shown.
        public async Task<IActionResult> Index(int? producerId)
        {
            if (User.IsInRole("Producer"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null) return Unauthorized();

                var producer = await _context.Producer.FirstOrDefaultAsync(s => s.UserId == userId);
                if (producer == null) return NotFound();

                // Load only the products belonging to this producer
                var producerProducts = await _context.Product
                    .Where(p => p.ProducerId == producer.ProducerId)
                    .Include(p => p.Producer)
                    .ToListAsync();
                return View(producerProducts);
            }
            else
            {
                // Build a query that can be filtered by producer if a producerId was supplied
                var query = _context.Product.Include(p => p.Producer).AsQueryable();
                if (producerId.HasValue)
                {
                    query = query.Where(p => p.ProducerId == producerId.Value);
                    var prod = await _context.Producer.FindAsync(producerId.Value);
                    // Pass the producer name to the view so it can show a "Showing products from X" banner
                    ViewBag.FilteredProducer = prod?.ProducerName;
                }
                var allProducts = await query.ToListAsync();
                return View(allProducts);
            }
        }

        // GET: /Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Product
                .Include(p => p.Producer)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // GET: /Products/Create
        // Only producers can access this page
        [Authorize(Roles = "Producer")]
        public IActionResult Create()
        {
            ViewData["ProducerId"] = new SelectList(_context.Producer, "ProducerId", "ProducerId");
            return View();
        }

        // POST: /Products/Create
        // The ProducerId is set automatically from the logged-in producer's profile,
        // so the user cannot create products for a different producer.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Producer")]
        public async Task<IActionResult> Create([Bind("ProductId,ProductName,Description,ImagePath,Price,StockQuantity,Category,DateAdded,IsActive")] Product product)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var producer = await _context.Producer.FirstOrDefaultAsync(p => p.UserId == userId);
            if (producer == null)
                return NotFound();

            // Assign the producer ID from the logged-in user's profile rather than from the form
            product.ProducerId = producer.ProducerId;
            ModelState.Remove("ProducerId"); // Remove validation error for the field we set manually

            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["ProducerId"] = new SelectList(_context.Producer, "ProducerId", "ProducerId", product.ProducerId);
            return View(product);
        }

        // GET: /Products/Edit/5
        // Only the producer who owns this product can edit it
        [Authorize(Roles = "Producer")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Product.FindAsync(id);
            if (product == null)
                return NotFound();

            // Ownership check - make sure the logged-in producer owns this product
            var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var producer = await _context.Producer.FirstOrDefaultAsync(p => p.UserId == userId);

            if (producer == null || product.ProducerId != producer.ProducerId)
                return Forbid();

            return View(product);
        }

        // POST: /Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Producer")]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,ProductName,Description,ImagePath,Price,StockQuantity,Category,DateAdded")] Product product)
        {
            if (id != product.ProductId)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var producer = await _context.Producer.FirstOrDefaultAsync(p => p.UserId == userId);
            if (producer == null)
                return NotFound();

            // Re-check ownership using AsNoTracking to avoid EF Core tracking conflicts
            var existingProduct = await _context.Product.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == id);
            if (existingProduct == null || existingProduct.ProducerId != producer.ProducerId)
                return Forbid();

            // Keep the original ProducerId - do not allow it to be changed via the form
            product.ProducerId = producer.ProducerId;
            ModelState.Remove("ProducerId");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["ProducerId"] = new SelectList(_context.Producer, "ProducerId", "ProducerId", product.ProducerId);
            return View(product);
        }

        // GET: /Products/Delete/5
        // Only the producer who owns this product can delete it
        [Authorize(Roles = "Producer")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Product
                .Include(p => p.Producer)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null)
                return NotFound();

            // Ownership check
            var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var producer = await _context.Producer.FirstOrDefaultAsync(p => p.UserId == userId);

            if (producer == null || product.ProducerId != producer.ProducerId)
                return Forbid();

            return View(product);
        }

        // POST: /Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Producer")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Product.FindAsync(id);
            if (product == null)
                return NotFound();

            // Final ownership check before actually deleting
            var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var producer = await _context.Producer.FirstOrDefaultAsync(p => p.UserId == userId);

            if (producer == null || product.ProducerId != producer.ProducerId)
                return Forbid();

            _context.Product.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Helper - checks whether a product with the given ID exists
        private bool ProductExists(int id)
        {
            return _context.Product.Any(e => e.ProductId == id);
        }
    }
}
