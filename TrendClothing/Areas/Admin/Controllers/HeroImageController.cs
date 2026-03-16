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
            var images = _db.SiteImages.ToList();

            // Hero
            ViewBag.Hero = images.FirstOrDefault(x => x.Key == "Hero")?.ImageUrl;

            // 4 Collection Images
            ViewBag.TopwearImg = images.FirstOrDefault(x => x.Key == "TopwearImg")?.ImageUrl;
            ViewBag.BottomwearImg = images.FirstOrDefault(x => x.Key == "BottomwearImg")?.ImageUrl;
            ViewBag.ActivewearImg = images.FirstOrDefault(x => x.Key == "ActivewearImg")?.ImageUrl;
            ViewBag.AccessoriesImg = images.FirstOrDefault(x => x.Key == "AccessoriesImg")?.ImageUrl;

            // Sub Category Images
            ViewBag.TshirtImg = images.FirstOrDefault(x => x.Key == "TshirtImg")?.ImageUrl;
            ViewBag.ShirtImg = images.FirstOrDefault(x => x.Key == "ShirtImg")?.ImageUrl;
            ViewBag.JoggerImg = images.FirstOrDefault(x => x.Key == "JoggerImg")?.ImageUrl;
            ViewBag.JeansImg = images.FirstOrDefault(x => x.Key == "JeansImg")?.ImageUrl;
            ViewBag.TrouserImg = images.FirstOrDefault(x => x.Key == "TrouserImg")?.ImageUrl;

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