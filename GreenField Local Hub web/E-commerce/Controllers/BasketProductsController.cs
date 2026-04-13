using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using E_commerce.Data;
using E_commerce.Models;

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
            if (id == null)
            {
                return NotFound();
            }

            var basketProduct = await _context.BasketProduct
                .Include(b => b.Basket)
                .Include(b => b.Product)
                .FirstOrDefaultAsync(m => m.BasketProductId == id);
            if (basketProduct == null)
            {
                return NotFound();
            }

            return View(basketProduct);
        }

        // GET: BasketProducts/Create
        public IActionResult Create()
        {
            ViewData["BasketId"] = new SelectList(_context.Basket, "BasketId", "BasketId");
            ViewData["ProductId"] = new SelectList(_context.Set<Product>(), "ProductId", "ProductId");
            return View();
        }

        // POST: BasketProducts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BasketProductId,ProductId,BasketId,Quantity,TotalPrice")] BasketProduct basketProduct)
        {
            if (ModelState.IsValid)
            {
                _context.Add(basketProduct);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BasketId"] = new SelectList(_context.Basket, "BasketId", "BasketId", basketProduct.BasketId);
            ViewData["ProductId"] = new SelectList(_context.Set<Product>(), "ProductId", "ProductId", basketProduct.ProductId);
            return View(basketProduct);
        }

        // GET: BasketProducts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var basketProduct = await _context.BasketProduct.FindAsync(id);
            if (basketProduct == null)
            {
                return NotFound();
            }
            ViewData["BasketId"] = new SelectList(_context.Basket, "BasketId", "BasketId", basketProduct.BasketId);
            ViewData["ProductId"] = new SelectList(_context.Set<Product>(), "ProductId", "ProductId", basketProduct.ProductId);
            return View(basketProduct);
        }

        // POST: BasketProducts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BasketProductId,ProductId,BasketId,Quantity,TotalPrice")] BasketProduct basketProduct)
        {
            if (id != basketProduct.BasketProductId)
            {
                return NotFound();
            }

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
            ViewData["BasketId"] = new SelectList(_context.Basket, "BasketId", "BasketId", basketProduct.BasketId);
            ViewData["ProductId"] = new SelectList(_context.Set<Product>(), "ProductId", "ProductId", basketProduct.ProductId);
            return View(basketProduct);
        }

        // GET: BasketProducts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var basketProduct = await _context.BasketProduct
                .Include(b => b.Basket)
                .Include(b => b.Product)
                .FirstOrDefaultAsync(m => m.BasketProductId == id);
            if (basketProduct == null)
            {
                return NotFound();
            }

            return View(basketProduct);
        }

        // POST: BasketProducts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var basketProduct = await _context.BasketProduct.FindAsync(id);
            if (basketProduct != null)
            {
                _context.BasketProduct.Remove(basketProduct);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BasketProductExists(int id)
        {
            return _context.BasketProduct.Any(e => e.BasketProductId == id);
        }
    }
}
