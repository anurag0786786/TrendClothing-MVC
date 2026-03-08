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
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;

        public ProfileController(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext db,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _db = db;
            _emailSender = emailSender;
        }

        // ===== GET =====
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            // If user is not logged in, redirect to login
            if (user == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            var profile = _db.UserProfiles
                .FirstOrDefault(x => x.UserId == user.Id);

            // Create profile if it doesn't exist
            if (profile == null)
            {
                profile = new UserProfile { UserId = user.Id };
                _db.UserProfiles.Add(profile);
                _db.SaveChanges();
            }

            var vm = new ProfileVM
            {
                ProfileId = profile.Id,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                FullName = profile.FullName ?? string.Empty,
                Address = profile.Address ?? string.Empty,
                City = profile.City ?? string.Empty,
                State = profile.State ?? string.Empty,
                PostalCode = profile.PostalCode ?? string.Empty,
                IsEmailConfirmed = user.EmailConfirmed
            };

            return View(vm);
        }

        // ===== POST =====
        [HttpPost]
        public async Task<IActionResult> Index(ProfileVM model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            // Update phone number
            user.PhoneNumber = model.PhoneNumber;
            await _userManager.UpdateAsync(user);

            // Find or create profile
            var profile = _db.UserProfiles.FirstOrDefault(x => x.Id == model.ProfileId)
                       ?? _db.UserProfiles.FirstOrDefault(x => x.UserId == user.Id);

            if (profile == null)
            {
                profile = new UserProfile { UserId = user.Id };
                _db.UserProfiles.Add(profile);
            }

            profile.FullName = model.FullName;
            profile.Address = model.Address;
            profile.City = model.City;
            profile.State = model.State;
            profile.PostalCode = model.PostalCode;

            _db.SaveChanges();

            TempData["ToastMessage"] = "Profile updated successfully ✓";
            return RedirectToAction(nameof(Index));
        }

        // ===== Send Verification Email =====
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
                "ConfirmEmail", "Profile",
                new { userId = user.Id, token = encodedToken },
                Request.Scheme
            );

            await _emailSender.SendEmailAsync(
                user.Email!,
                "Verify your email – TrendClothing",
                $"Click here to verify your email:<br/><a href='{callbackUrl}'>Verify Email</a>"
            );

            TempData["ToastMessage"] = "Verification email sent 📧";
            return RedirectToAction("Index");
        }

        // ===== Confirm Email =====
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

            TempData["ToastMessage"] = result.Succeeded
                ? "Email verified successfully ✅"
                : "Invalid or expired verification link ❌";

            return RedirectToAction("Index");
        }
    }
}