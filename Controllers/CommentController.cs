using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurkeyCityGuide.Data;
using TurkeyCityGuide.Models;

namespace TurkeyCityGuide.Controllers
{
    [Authorize]
    public class CommentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public CommentController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // POST: Comment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCommentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                string? photoPath = null;
                if (model.Photo != null && model.Photo.Length > 0)
                {
                    // Dosya adı oluştur
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.Photo.FileName);
                    var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "comments");
                    
                    if (!Directory.Exists(uploadDir))
                        Directory.CreateDirectory(uploadDir);

                    var filePath = Path.Combine(uploadDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Photo.CopyToAsync(stream);
                    }
                    photoPath = "/images/comments/" + fileName;
                }

                var comment = new Comment
                {
                    Content = model.Content,
                    Rating = model.Rating,
                    Category = model.Category,
                    CityId = model.CityId,
                    DistrictId = model.DistrictId, // Opsiyonel
                    AppUserId = user.Id,
                    CreatedAt = DateTime.Now,
                    PhotoPath = photoPath
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                return RedirectToAction("Detail", "City", new { id = model.CityName });
            }

            return RedirectToAction("Detail", "City", new { id = model.CityName });
        }

        // GET: Comment/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var comment = await _context.Comments
                .Include(c => c.City)
                .Include(c => c.District)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null) return NotFound();

            // Sadece yorum sahibi düzenleyebilir
            if (comment.AppUserId != user.Id) return Forbid();

            var model = new EditCommentViewModel
            {
                Id = comment.Id,
                Content = comment.Content,
                Rating = comment.Rating,
                Category = comment.Category,
                ExistingPhotoPath = comment.PhotoPath,
                CityName = comment.City.Name,
                DistrictName = comment.District?.Name
            };

            return View(model);
        }

        // POST: Comment/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditCommentViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var comment = await _context.Comments.FindAsync(model.Id);
            if (comment == null) return NotFound();

            if (comment.AppUserId != user.Id) return Forbid();

            // Verileri güncelle
            comment.Content = model.Content;
            comment.Rating = model.Rating;
            comment.Category = model.Category;

            // Fotoğraf işlemleri
            if (model.Photo != null && model.Photo.Length > 0)
            {
                // Yeni fotoğraf yükle
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.Photo.FileName);
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "comments");
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);
                var filePath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Photo.CopyToAsync(stream);
                }

                // Eski fotoğrafı sil (Opsiyonel ama iyi olur)
                // if (!string.IsNullOrEmpty(comment.PhotoPath)) ...

                comment.PhotoPath = "/images/comments/" + fileName;
            }
            else if (model.DeletePhoto) 
            {
                 // Kullanıcı fotoğrafı silmek isterse
                 comment.PhotoPath = null;
                 // Dosyayı da silebiliriz...
            }

            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Detail", "City", new { id = model.CityName });
        }

        // GET: Comment/MyComments
        [HttpGet]
        public async Task<IActionResult> MyComments()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var comments = await _context.Comments
                .Include(c => c.City)
                .Include(c => c.District)
                .Where(c => c.AppUserId == user.Id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(comments);
        }
    }

    public class EditCommentViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = null!;
        public int Rating { get; set; }
        public string Category { get; set; } = null!;
        public IFormFile? Photo { get; set; }
        public string? ExistingPhotoPath { get; set; }
        public bool DeletePhoto { get; set; }
        
        public string CityName { get; set; } = null!; // Redirect için
        public string? DistrictName { get; set; } // Bilgi için
    }

    // View Model
    public class CreateCommentViewModel
    {
        public int CityId { get; set; }
        public string CityName { get; set; } = null!;
        public int? DistrictId { get; set; }
        public string Content { get; set; } = null!;
        public int Rating { get; set; }
        public string Category { get; set; } = null!; // Yemek, Turistik, Genel
        public IFormFile? Photo { get; set; }
    }
}
