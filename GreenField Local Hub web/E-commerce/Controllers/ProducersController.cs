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
    // Handles the public producer listing page and admin CRUD for producer profiles.
    // The Index page is accessible to everyone; Create/Edit/Delete are for admin use.
    public class ProducersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProducersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Producers
        // Returns all producers for the public-facing producer listing page
        public async Task<IActionResult> Index()
        {
            return View(await _context.Producer.ToListAsync());
        }

        // GET: /Producers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var producer = await _context.Producer
                .FirstOrDefaultAsync(m => m.ProducerId == id);
            if (producer == null)
                return NotFound();

            return View(producer);
        }

        // GET: /Producers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Producers/Create
        // Bind only the fields we want to allow from the form to prevent overposting
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProducerId,UserId,ProducerName,PhoneNumber,ProductDescription,Location,ProducerInfo,DateJoined,IsVerified")] Producer producer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(producer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(producer);
        }

        // GET: /Producers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var producer = await _context.Producer.FindAsync(id);
            if (producer == null)
                return NotFound();

            return View(producer);
        }

        // POST: /Producers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProducerId,UserId,ProducerName,PhoneNumber,ProductDescription,Location,ProducerInfo,DateJoined,IsVerified")] Producer producer)
        {
            if (id != producer.ProducerId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(producer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProducerExists(producer.ProducerId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(producer);
        }

        // GET: /Producers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var producer = await _context.Producer
                .FirstOrDefaultAsync(m => m.ProducerId == id);
            if (producer == null)
                return NotFound();

            return View(producer);
        }

        // POST: /Producers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producer = await _context.Producer.FindAsync(id);
            if (producer != null)
                _context.Producer.Remove(producer);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Helper - checks whether a producer with the given ID exists
        private bool ProducerExists(int id)
        {
            return _context.Producer.Any(e => e.ProducerId == id);
        }
    }
}
