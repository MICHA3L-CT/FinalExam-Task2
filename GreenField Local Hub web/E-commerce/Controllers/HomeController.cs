using System.Diagnostics;
using E_commerce.Data;
using E_commerce.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_commerce.Controllers
{
    // Handles all the general informational pages of the site:
    // home page, about, privacy policy, loyalty programme, and terms and conditions.
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        // Constructor receives the logger and database context via dependency injection
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger  = logger;
            _context = context;
        }

        // GET: / or /Home/Index
        // Loads up to 3 producers from the database and passes them to the home page view
        // so the "Meet Our Producers" section shows real data instead of static content.
        public async Task<IActionResult> Index()
        {
            var producers = await _context.Producer.Take(3).ToListAsync();
            return View(producers);
        }

        // GET: /Home/About
        public IActionResult About() => View();

        // GET: /Home/Privacy
        public IActionResult Privacy() => View();

        // GET: /Home/Loyalty
        public IActionResult Loyalty() => View();

        // GET: /Home/TermsAndConditions
        public IActionResult TermsAndConditions() => View();

        // GET: /Home/Error
        // Disabled response caching so the error page is never served from cache
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Pass the current request ID to the view so it can be shown for debugging
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
