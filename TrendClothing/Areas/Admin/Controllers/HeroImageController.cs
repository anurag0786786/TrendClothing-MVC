using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendClothing.Data;
using TrendClothing.Models;
using TrendClothing.Utility;

namespace TrendClothing.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class HeroImageController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly CloudinaryService _cloudinary;

        public HeroImageController(ApplicationDbContext db, CloudinaryService cloudinary)
        {
            _db = db;
            _cloudinary = cloudinary;
        }

        public IActionResult Index()
        {
            // Load all 4 site images keyed by name
            var images = _db.SiteImages.ToList();
            ViewBag.Hero = images.FirstOrDefault(x => x.Key == "Hero")?.ImageUrl;
            ViewBag.Men = images.FirstOrDefault(x => x.Key == "Men")?.ImageUrl;
            ViewBag.Women = images.FirstOrDefault(x => x.Key == "Women")?.ImageUrl;
            ViewBag.Children = images.FirstOrDefault(x => x.Key == "Children")?.ImageUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, string key)
        {
            if (file == null || string.IsNullOrEmpty(key))
            {
                TempData["ToastMessage"] = "Please select an image ❌";
                return RedirectToAction("Index");
            }

            var imageUrl = await _cloudinary.UploadImageAsync(file);

            var existing = _db.SiteImages.FirstOrDefault(x => x.Key == key);
            if (existing == null)
            {
                _db.SiteImages.Add(new SiteImage { Key = key, ImageUrl = imageUrl });
            }
            else
            {
                existing.ImageUrl = imageUrl;
            }

            _db.SaveChanges();
            TempData["ToastMessage"] = $"{key} image updated ✅";
            return RedirectToAction("Index");
        }
    }
}