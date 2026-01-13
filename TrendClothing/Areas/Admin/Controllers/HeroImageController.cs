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
            var hero = _db.HeroImages.FirstOrDefault();
            return View(hero);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null)
            {
                TempData["error"] = "Please select an image";
                return RedirectToAction("Index");
            }

            var imageUrl = await _cloudinary.UploadImageAsync(file);

            var hero = _db.HeroImages.FirstOrDefault();
            if (hero == null)
            {
                hero = new HeroImage { ImageUrl = imageUrl };
                _db.HeroImages.Add(hero);
            }
            else
            {
                hero.ImageUrl = imageUrl;
            }

            _db.SaveChanges();
            TempData["success"] = "Hero image updated";
            return RedirectToAction("Index");
        }
    }
}
