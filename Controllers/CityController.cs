using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurkeyCityGuide.Data;
using TurkeyCityGuide.Models;

namespace TurkeyCityGuide.Controllers
{
    public class CityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CityController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Detail(string id, int? districtId, string? category)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index", "Home");
            }

            // Şehri isme göre bul
            var city = await _context.Cities
                .Include(c => c.Districts)
                .Include(c => c.Comments)
                    .ThenInclude(com => com.AppUser)
                .Include(c => c.Comments)
                    .ThenInclude(com => com.District)
                .Include(c => c.Photos)
                .FirstOrDefaultAsync(c => c.Name == id);

            if (city == null)
            {
                // Slug eşleştirme denemesi (örn: Adiyaman -> Adıyaman)
                // Tüm şehir isimlerini çekip hafızada karşılaştır (Performans için sadece ID ve Name çekiyoruz)
                var allCities = await _context.Cities.Select(c => new { c.Id, c.Name }).ToListAsync();
                
                string NormalizeName(string name)
                {
                    return name.ToLower()
                        .Replace("ı", "i")
                        .Replace("İ", "i")
                        .Replace("ğ", "g")
                        .Replace("ü", "u")
                        .Replace("ş", "s")
                        .Replace("ö", "o")
                        .Replace("ç", "c")
                        .Replace(" ", "-");
                }

                var normalizedId = NormalizeName(id);
                var match = allCities.FirstOrDefault(c => NormalizeName(c.Name) == normalizedId);

                if (match != null)
                {
                    city = await _context.Cities
                        .Include(c => c.Districts)
                        .Include(c => c.Comments)
                            .ThenInclude(com => com.AppUser)
                        .Include(c => c.Comments)
                            .ThenInclude(com => com.District)
                        .Include(c => c.Photos)
                        .FirstOrDefaultAsync(c => c.Id == match.Id);
                }
            }

            if (city == null)
            {
                return NotFound();
            }

            // İlçe bazlı filtreleme
            if (districtId.HasValue)
            {
                city.Comments = city.Comments
                    .Where(c => c.DistrictId == districtId.Value)
                    .ToList();
            }

            // Kategori bazlı filtreleme
            if (!string.IsNullOrEmpty(category))
            {
                city.Comments = city.Comments
                    .Where(c => c.Category == category)
                    .ToList();
            }

            // Tarihe göre sıralama (En yeni en üstte)
            city.Comments = city.Comments.OrderByDescending(c => c.CreatedAt).ToList();

            ViewBag.SelectedDistrictId = districtId;
            ViewBag.SelectedCategory = category;

            return View(city);
        }

        // AJAX: İlçe bazlı yorumları getir
        [HttpGet]
        public async Task<IActionResult> GetCommentsByDistrict(int cityId, int? districtId, string? category)
        {
            var query = _context.Comments
                .Include(c => c.AppUser)
                .Include(c => c.District)
                .Where(c => c.CityId == cityId)
                .AsQueryable();

            // İlçe filtresi - districtId varsa sadece o ilçe, yoksa tüm yorumlar
            if (districtId.HasValue && districtId.Value > 0)
            {
                query = query.Where(c => c.DistrictId == districtId.Value);
            }
            // districtId null veya 0 ise tüm yorumları göster (filtreleme yapma)

            // Kategori filtresi
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(c => c.Category == category);
            }

            var comments = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return PartialView("_CommentList", comments);
        }

        [HttpGet]
        public async Task<IActionResult> GetCityNames()
        {
            var cities = await _context.Cities
                .Select(c => c.Name)
                .OrderBy(n => n)
                .ToListAsync();
            return Json(cities);
        }
    }
}

