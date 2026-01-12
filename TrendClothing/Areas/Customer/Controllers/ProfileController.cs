using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
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
        private readonly IEmailSender _emailSender;

        public ProfileController(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext db,IEmailSender emailSender)
        {
            _userManager = userManager;
            _db = db;
            _emailSender = emailSender;
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
                PostalCode = profile.PostalCode,
                IsEmailConfirmed = user.EmailConfirmed
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
        [HttpPost]
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SendVerificationEmail()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Index");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebUtility.UrlEncode(token);

            var callbackUrl = Url.Action(
                "ConfirmEmail",
                "Profile",
                new { userId = user.Id, token = encodedToken },
                Request.Scheme
            );

            await _emailSender.SendEmailAsync(
                user.Email,
                "Verify your email – TrendClothing",
                $"Click here to verify your email:<br/><a href='{callbackUrl}'>Verify Email</a>"
            );

            TempData["ToastMessage"] = "Verification email sent 📧";
            TempData["ToastColor"] = "#0d6efd";

            return RedirectToAction("Index");
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
                return RedirectToAction("Index", "Home");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var decodedToken = WebUtility.UrlDecode(token);

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (result.Succeeded)
            {
                TempData["ToastMessage"] = "Email verified successfully ✅";
                TempData["ToastColor"] = "green";
                return RedirectToAction("Index");
            }

            TempData["ToastMessage"] = "Invalid or expired verification link ❌";
            TempData["ToastColor"] = "red";
            return RedirectToAction("Index");
        }


    }
}
