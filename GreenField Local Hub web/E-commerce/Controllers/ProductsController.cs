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
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index(int? producerId)
        {
            if (User.IsInRole("Producer"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null) return Unauthorized();
                var producer = await _context.Producer.FirstOrDefaultAsync(s => s.UserId == userId);
                if (producer == null) return NotFound();
                var producerProducts = await _context.Product
                    .Where(p => p.ProducerId == producer.ProducerId)
                    .Include(p => p.Producer)
                    .ToListAsync();
                return View(producerProducts);
            }
            else
            {
                var query = _context.Product.Include(p => p.Producer).AsQueryable();
                if (producerId.HasValue)
                {
                    query = query.Where(p => p.ProducerId == producerId.Value);
                    var prod = await _context.Producer.FindAsync(producerId.Value);
                    ViewBag.FilteredProducer = prod?.ProducerName;
                }
                var allProducts = await query.ToListAsync();
                return View(allProducts);
            }
        }

        // GET: Products/Details/5
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

        // GET: Products/Create
        [Authorize(Roles = "Producer")]
        public IActionResult Create()
        {
            ViewData["ProducerId"] = new SelectList(_context.Producer, "ProducerId", "ProducerId");
            return View();
        }

        // POST: Products/Create
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

            product.ProducerId = producer.ProducerId;
            ModelState.Remove("ProducerId");

            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["ProducerId"] = new SelectList(_context.Producer, "ProducerId", "ProducerId", product.ProducerId);
            return View(product);
        }

        // GET: Products/Edit/5
        [Authorize(Roles = "Producer")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Product.FindAsync(id);
            if (product == null)
                return NotFound();

            // Ownership check
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var producer = await _context.Producer.FirstOrDefaultAsync(p => p.UserId == userId);

            if (producer == null || product.ProducerId != producer.ProducerId)
                return Forbid();

            return View(product);
        }

        // POST: Products/Edit/5
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

            // Ownership check
            var existingProduct = await _context.Product.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == id);
            if (existingProduct == null || existingProduct.ProducerId != producer.ProducerId)
                return Forbid();

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

        // GET: Products/Delete/5
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var producer = await _context.Producer.FirstOrDefaultAsync(p => p.UserId == userId);

            if (producer == null || product.ProducerId != producer.ProducerId)
                return Forbid();

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Producer")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Product.FindAsync(id);
            if (product == null)
                return NotFound();

            // Ownership check
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var producer = await _context.Producer.FirstOrDefaultAsync(p => p.UserId == userId);

            if (producer == null || product.ProducerId != producer.ProducerId)
                return Forbid();

            _context.Product.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Product.Any(e => e.ProductId == id);
        }
    }
}