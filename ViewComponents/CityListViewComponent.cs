using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurkeyCityGuide.Data;
using TurkeyCityGuide.Models;

namespace TurkeyCityGuide.ViewComponents
{
    public class CityListViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public CityListViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var cities = await _context.Cities
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name, c.PlateCode }) // Only needed fields
                .ToListAsync();
            
            // Map to City for the view (or use a ViewModel, but City is fine here if minimal)
            var model = cities.Select(c => new City { Id = c.Id, Name = c.Name, PlateCode = c.PlateCode }).ToList();

            return View(model);
        }
    }
}
