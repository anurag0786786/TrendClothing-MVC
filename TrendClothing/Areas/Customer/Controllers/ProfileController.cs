using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrendClothing.Data;
using TrendClothing.Models;
using TrendClothing.Models.ViewModels;

namespace TrendClothing.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ProfileController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _db;

        public ProfileController(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }


        // ===== GET =====
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var profile = _db.UserProfiles
                .FirstOrDefault(x => x.UserId == user.Id);

            // ✅ IMPORTANT FIX
            if (profile == null)
            {
                profile = new UserProfile
                {
                    UserId = user.Id
                };
            }

            var vm = new ProfileVM
            {
                ProfileId = profile.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FullName = profile.FullName,
                Address = profile.Address,
                City = profile.City,
                State = profile.State,
                PostalCode = profile.PostalCode
            };

            return View(vm);
        }


        [HttpPost]
        public async Task<IActionResult> Index(ProfileVM model)
        {
            var user = await _userManager.GetUserAsync(User);

            // 🔹 Identity: only phone/email (optional)
            user.PhoneNumber = model.PhoneNumber;
            await _userManager.UpdateAsync(user);

            // 🔹 UserProfile update
            var profile = _db.UserProfiles.First(x => x.Id == model.ProfileId);

            profile.FullName = model.FullName;
            profile.Address = model.Address;
            profile.City = model.City;
            profile.State = model.State;
            profile.PostalCode = model.PostalCode;

            _db.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

    }
}
