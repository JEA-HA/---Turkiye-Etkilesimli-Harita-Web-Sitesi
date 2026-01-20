using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurkeyCityGuide.Data;
using TurkeyCityGuide.Models;

namespace TurkeyCityGuide.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Admin
        public IActionResult Index()
        {
            var cities = _context.Cities
                .Include(c => c.Districts)
                .Include(c => c.Comments)
                .ToList();

            return View(cities);
        }

        // GET: Admin/Cities
        public IActionResult Cities()
        {
            var cities = _context.Cities
                .Include(c => c.Districts)
                .OrderBy(c => c.PlateCode)
                .ToList();

            // İlçe sayısını DistrictCount'a senkronize et
            foreach (var city in cities)
            {
                if (city.Districts.Count != city.DistrictCount)
                {
                    city.DistrictCount = city.Districts.Count;
                }
            }
            _context.SaveChanges();

            return View(cities);
        }

        // GET: Admin/City/Create
        public IActionResult CreateCity()
        {
            return View();
        }

        // POST: Admin/City/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCity(City city, IFormFile? districtMapSvg, List<IFormFile> cityPhotos)
        {
            if (ModelState.IsValid)
            {
                // DistrictCount'u başlangıçta 0 yap (SVG yüklenirse güncellenecek)
                city.DistrictCount = 0;
                
                // Önce city'yi kaydet ki Id oluşsun
                _context.Add(city);
                await _context.SaveChangesAsync();

                // SVG harita dosyası yükle ve ilçeleri çıkar
                if (districtMapSvg != null && districtMapSvg.Length > 0)
                {
                    // Sadece SVG dosyası kabul et
                    var extension = Path.GetExtension(districtMapSvg.FileName).ToLower();
                    if (extension != ".svg")
                    {
                        ModelState.AddModelError("districtMapSvg", "Sadece SVG formatı kabul edilir.");
                        // Hata durumunda city'yi geri sil
                        _context.Cities.Remove(city);
                        await _context.SaveChangesAsync();
                        return View(city);
                    }

                    var citySlug = city.Name.ToLower();
                    var fileName = $"{citySlug}.svg";
                    var folderPath = Path.Combine(_environment.WebRootPath, "maps", "districts");
                    Directory.CreateDirectory(folderPath);

                    var filePath = Path.Combine(folderPath, fileName);

                    // SVG dosyasını kaydet
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await districtMapSvg.CopyToAsync(stream);
                    }

                    // SVG'den ilçe adlarını çıkar (artık city.Id mevcut)
                    await ExtractDistrictsFromSvg(filePath, city.Id);
                    
                    // İlçe sayısını güncelle
                    var districtCount = _context.Districts.Count(d => d.CityId == city.Id);
                    city.DistrictCount = districtCount;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // SVG yüklenmediyse DistrictCount'u 0 yap
                    city.DistrictCount = 0;
                    await _context.SaveChangesAsync();
                }

                // Fotoğrafları Kaydet
                if (cityPhotos != null && cityPhotos.Count > 0)
                {
                    var citySlug = city.Name.ToLower();
                    var folderPath = Path.Combine(_environment.WebRootPath, "images", "cities", citySlug);
                    Directory.CreateDirectory(folderPath);

                    foreach (var file in cityPhotos)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                            var filePath = Path.Combine(folderPath, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var photo = new CityPhoto
                            {
                                CityId = city.Id,
                                ImagePath = $"/images/cities/{citySlug}/{fileName}",
                                Caption = city.Name // Varsayılan başlık
                            };
                            _context.CityPhotos.Add(photo);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Cities));
            }
            return View(city);
        }

        // GET: Admin/City/Edit/5
        public async Task<IActionResult> EditCity(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var city = await _context.Cities.Include(c => c.Photos).FirstOrDefaultAsync(m => m.Id == id);
            if (city == null)
            {
                return NotFound();
            }

            return View(city);
        }

        // POST: Admin/City/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCity(int id, City city, IFormFile? districtMapSvg, List<IFormFile> cityPhotos)
        {
            if (id != city.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // SVG harita dosyası yükle ve ilçeleri güncelle
                if (districtMapSvg != null && districtMapSvg.Length > 0)
                {
                    // Sadece SVG dosyası kabul et
                    var extension = Path.GetExtension(districtMapSvg.FileName).ToLower();
                    if (extension != ".svg")
                    {
                        ModelState.AddModelError("districtMapSvg", "Sadece SVG formatı kabul edilir.");
                        return View(city);
                    }

                    var citySlug = city.Name.ToLower();
                    var fileName = $"{citySlug}.svg";
                    var folderPath = Path.Combine(_environment.WebRootPath, "maps", "districts");
                    Directory.CreateDirectory(folderPath);

                    var filePath = Path.Combine(folderPath, fileName);

                    // SVG dosyasını kaydet
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await districtMapSvg.CopyToAsync(stream);
                    }

                    // Mevcut ilçelere bağlı yorumların DistrictId'sini null yap
                    var existingDistrictIds = _context.Districts
                        .Where(d => d.CityId == city.Id)
                        .Select(d => d.Id)
                        .ToList();
                    
                    if (existingDistrictIds.Any())
                    {
                        var commentsWithDistricts = _context.Comments
                            .Where(c => c.DistrictId.HasValue && existingDistrictIds.Contains(c.DistrictId.Value))
                            .ToList();
                        
                        foreach (var comment in commentsWithDistricts)
                        {
                            comment.DistrictId = null;
                        }
                        if (commentsWithDistricts.Any())
                        {
                            await _context.SaveChangesAsync();
                        }
                    }

                    // Mevcut ilçeleri sil ve SVG'den yeni ilçeleri çıkar
                    var existingDistricts = _context.Districts.Where(d => d.CityId == city.Id).ToList();
                    _context.Districts.RemoveRange(existingDistricts);
                    await _context.SaveChangesAsync();

                    await ExtractDistrictsFromSvg(filePath, city.Id);
                }

                // Yeni Fotoğrafları Kaydet
                if (cityPhotos != null && cityPhotos.Count > 0)
                {
                    var citySlug = city.Name.ToLower();
                    var folderPath = Path.Combine(_environment.WebRootPath, "images", "cities", citySlug);
                    Directory.CreateDirectory(folderPath);

                    foreach (var file in cityPhotos)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                            var filePath = Path.Combine(folderPath, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var photo = new CityPhoto
                            {
                                CityId = city.Id,
                                ImagePath = $"/images/cities/{citySlug}/{fileName}",
                                Caption = city.Name
                            };
                            _context.CityPhotos.Add(photo);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                try
                {
                    // Veritabanından mevcut city'yi çek
                    var existingCity = await _context.Cities.FindAsync(city.Id);
                    if (existingCity == null)
                    {
                        return NotFound();
                    }

                    // Gelen değerleri mevcut city'ye kopyala
                    existingCity.Name = city.Name;
                    existingCity.PlateCode = city.PlateCode;
                    existingCity.Region = city.Region;
                    existingCity.Population = city.Population;
                    existingCity.AreaKm2 = city.AreaKm2;
                    existingCity.Elevation = city.Elevation;
                    existingCity.Description = city.Description;

                    // İlçe sayısını Districts koleksiyonundan güncelle
                    var districtCount = _context.Districts.Count(d => d.CityId == city.Id);
                    existingCity.DistrictCount = districtCount;
                    
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CityExists(city.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Cities));
            }
            return View(city);
        }

        // GET: Admin/City/Delete/5
        public async Task<IActionResult> DeleteCity(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var city = await _context.Cities
                .FirstOrDefaultAsync(m => m.Id == id);
            if (city == null)
            {
                return NotFound();
            }

            return View(city);
        }

        // POST: Admin/City/Delete/5
        [HttpPost, ActionName("DeleteCity")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCityConfirmed(int id)
        {
            var city = await _context.Cities.FindAsync(id);
            if (city != null)
            {
                _context.Cities.Remove(city);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Cities));
        }

        // GET: Admin/Districts
        public IActionResult Districts(int? cityId)
        {
            var query = _context.Districts.Include(d => d.City).AsQueryable();

            if (cityId.HasValue)
            {
                query = query.Where(d => d.CityId == cityId.Value);
            }

            var districts = query.OrderBy(d => d.City.Name).ThenBy(d => d.Name).ToList();
            ViewBag.Cities = _context.Cities.OrderBy(c => c.Name).ToList();
            ViewBag.SelectedCityId = cityId;

            return View(districts);
        }

        // İlçe ekleme/düzenleme sayfaları kaldırıldı - SVG'den otomatik oluşturuluyor

        // GET: Admin/Comments
        public IActionResult Comments()
        {
            var comments = _context.Comments
                .Include(c => c.City)
                .Include(c => c.District)
                .Include(c => c.AppUser)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            return View(comments);
        }

        // POST: Admin/Comment/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment != null)
            {
                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Comments));
        }

        // GET: Admin/Photos
        public IActionResult Photos(int? cityId)
        {
            var query = _context.CityPhotos.Include(p => p.City).AsQueryable();

            if (cityId.HasValue)
            {
                query = query.Where(p => p.CityId == cityId.Value);
            }

            var photos = query.OrderByDescending(p => p.CreatedAt).ToList();
            ViewBag.Cities = _context.Cities.OrderBy(c => c.Name).ToList();
            ViewBag.SelectedCityId = cityId;

            return View(photos);
        }

        // POST: Admin/Photo/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhoto(int cityId, IFormFile file, string? caption)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Dosya seçilmedi.");
            }

            var city = await _context.Cities.FindAsync(cityId);
            if (city == null)
            {
                return NotFound();
            }

            // Dosya adını oluştur
            var citySlug = city.Name.ToLower();
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var folderPath = Path.Combine(_environment.WebRootPath, "images", "cities", citySlug);
            Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var photo = new CityPhoto
            {
                CityId = cityId,
                ImagePath = $"/images/cities/{citySlug}/{fileName}",
                Caption = caption
            };

            _context.CityPhotos.Add(photo);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Photos), new { cityId });
        }

        // POST: Admin/Photo/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            var photo = await _context.CityPhotos.FindAsync(id);
            if (photo != null)
            {
                // Fiziksel dosyayı sil
                var filePath = Path.Combine(_environment.WebRootPath, photo.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                _context.CityPhotos.Remove(photo);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Photos));
        }

        // POST: Admin/Photo/DeleteAjax/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePhotoAjax(int id)
        {
            var photo = await _context.CityPhotos.FindAsync(id);
            if (photo != null)
            {
                // Fiziksel dosyayı sil
                var filePath = Path.Combine(_environment.WebRootPath, photo.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                _context.CityPhotos.Remove(photo);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Fotoğraf bulunamadı." });
        }

        private bool CityExists(int id)
        {
            return _context.Cities.Any(e => e.Id == id);
        }

        // SVG dosyasından ilçe adlarını çıkar
        private async Task ExtractDistrictsFromSvg(string svgFilePath, int cityId)
        {
            try
            {
                var svgContent = await System.IO.File.ReadAllTextAsync(svgFilePath);
                var districts = new List<District>();
                var districtNames = new HashSet<string>();

                // XML parser kullanarak SVG'yi parse et
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(svgContent);

                // Namespace manager oluştur (SVG namespace için)
                var nsmgr = new System.Xml.XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace("svg", "http://www.w3.org/2000/svg");

                // Farklı yöntemlerle path elementlerini bul
                System.Xml.XmlNodeList? pathNodes = null;

                // Yöntem 1: Normal path elementleri
                pathNodes = xmlDoc.SelectNodes("//path[@name] | //path[@id]");
                
                // Yöntem 2: Namespace ile
                if (pathNodes == null || pathNodes.Count == 0)
                {
                    pathNodes = xmlDoc.SelectNodes("//svg:path[@name] | //svg:path[@id]", nsmgr);
                }
                
                // Yöntem 3: Local-name ile
                if (pathNodes == null || pathNodes.Count == 0)
                {
                    pathNodes = xmlDoc.SelectNodes("//*[local-name()='path'][@name] | //*[local-name()='path'][@id]");
                }

                // Yöntem 4: Tüm path elementlerini al ve attribute'ları kontrol et
                if (pathNodes == null || pathNodes.Count == 0)
                {
                    pathNodes = xmlDoc.SelectNodes("//path | //svg:path | //*[local-name()='path']", nsmgr);
                }
                
                // Yöntem 5: g (group) elementlerini kontrol et - bazı SVG'lerde ilçeler group içinde olabilir
                var groupNodes = xmlDoc.SelectNodes("//g[@name] | //g[@id] | //svg:g[@name] | //svg:g[@id] | //*[local-name()='g'][@name] | //*[local-name()='g'][@id]", nsmgr);
                if (groupNodes != null && groupNodes.Count > 0)
                {
                    foreach (System.Xml.XmlNode groupNode in groupNodes)
                    {
                        var groupName = groupNode.Attributes?["name"]?.Value ?? groupNode.Attributes?["id"]?.Value;
                        if (!string.IsNullOrWhiteSpace(groupName))
                        {
                            groupName = groupName.Trim();
                            if (!districtNames.Contains(groupName))
                            {
                                districtNames.Add(groupName);
                                var exists = _context.Districts.Any(d => d.CityId == cityId && d.Name == groupName);
                                if (!exists)
                                {
                                    districts.Add(new District
                                    {
                                        Name = groupName,
                                        CityId = cityId
                                    });
                                }
                            }
                        }
                    }
                }

                if (pathNodes != null && pathNodes.Count > 0)
                {
                    foreach (System.Xml.XmlNode pathNode in pathNodes)
                    {
                        // Önce name, sonra id, sonra data-name, sonra title attribute'larını kontrol et
                        var districtName = pathNode.Attributes?["name"]?.Value 
                            ?? pathNode.Attributes?["id"]?.Value
                            ?? pathNode.Attributes?["data-name"]?.Value
                            ?? pathNode.Attributes?["title"]?.Value
                            ?? pathNode.Attributes?["aria-label"]?.Value;
                        
                        // Eğer hala boşsa, parent element'ten al
                        if (string.IsNullOrWhiteSpace(districtName))
                        {
                            var parent = pathNode.ParentNode;
                            if (parent != null)
                            {
                                districtName = parent.Attributes?["name"]?.Value 
                                    ?? parent.Attributes?["id"]?.Value;
                            }
                        }
                        
                        if (!string.IsNullOrWhiteSpace(districtName))
                        {
                            // Temizle ve normalize et
                            districtName = districtName.Trim();

                            // Filter out "paintmaps" and other non-district texts
                            if (districtName.IndexOf("paintmaps", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                districtName.IndexOf("Created with", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                districtName.IndexOf("Terms of Use", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                continue;
                            }
                            
                            // Tekrar eden isimleri önle
                            if (!districtNames.Contains(districtName))
                            {
                                districtNames.Add(districtName);
                                
                                // Aynı isimde ilçe zaten var mı kontrol et
                                var exists = _context.Districts.Any(d => d.CityId == cityId && d.Name == districtName);
                                if (!exists)
                                {
                                    districts.Add(new District
                                    {
                                        Name = districtName,
                                        CityId = cityId
                                    });
                                }
                            }
                        }
                    }
                }

                // Eğer path'lerden isim bulunamadıysa, text elementlerini kontrol et
                if (districts.Count == 0)
                {
                    var textNodes = xmlDoc.SelectNodes("//text | //svg:text | //*[local-name()='text']", nsmgr);
                    if (textNodes != null)
                    {
                        foreach (System.Xml.XmlNode textNode in textNodes)
                        {
                            var textContent = textNode.InnerText?.Trim();
                            if (!string.IsNullOrWhiteSpace(textContent) && textContent.Length < 50) // Çok uzun metinler muhtemelen ilçe adı değil
                            {
                                // Filter text content as well
                                if (textContent.IndexOf("paintmaps", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    textContent.IndexOf("Created with", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    continue;
                                }

                                if (!districtNames.Contains(textContent))
                                {
                                    districtNames.Add(textContent);
                                    var exists = _context.Districts.Any(d => d.CityId == cityId && d.Name == textContent);
                                    if (!exists)
                                    {
                                        districts.Add(new District
                                        {
                                            Name = textContent,
                                            CityId = cityId
                                        });
                                    }
                                }
                            }
                        }
                    }
                }

                if (districts.Any())
                {
                    _context.Districts.AddRange(districts);
                    await _context.SaveChangesAsync();
                    
                    // İlçe sayısını güncelle
                    var city = await _context.Cities.FindAsync(cityId);
                    if (city != null)
                    {
                        city.DistrictCount = districts.Count;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda logla ama devam et
                // İlçeler manuel olarak da eklenebilir
                System.Diagnostics.Debug.WriteLine($"SVG parsing hatası: {ex.Message}");
            }
        }
    }
}
