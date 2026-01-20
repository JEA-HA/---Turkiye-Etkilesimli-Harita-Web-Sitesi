using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurkeyCityGuide.Data;
using TurkeyCityGuide.Models;

namespace TurkeyCityGuide.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var cities = await _context.Cities.OrderBy(c => c.Name).ToListAsync();

        // Get random photos for background
        var randomPhotos = await _context.CityPhotos
            .Include(p => p.City)
            .OrderBy(r => Guid.NewGuid())
            .Take(10)
            .ToListAsync();

        ViewBag.BackgroundPhotos = randomPhotos;

        return View(cities);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
