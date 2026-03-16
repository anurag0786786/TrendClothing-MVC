using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;
using TrendClothing.Models.ViewModels;

namespace TrendClothing.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IUnitofWork _unitOfWork;

        // ✅ FIX: Removed ApplicationDbContext _db — UserProfile ab UnitOfWork se access hoga
        public ProfileController(
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender,
            IUnitofWork unitOfWork)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _unitOfWork = unitOfWork;
        }

        // ── GET ──
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            // ✅ FIX: UnitOfWork se UserProfile load karo
            var profile = _unitOfWork.UserProfile.FirstOrDefault(x => x.UserId == user.Id);
            if (profile == null)
            {
                profile = new UserProfile { UserId = user.Id };
                _unitOfWork.UserProfile.Add(profile);
                _unitOfWork.Save();
            }

            var savedAddress = _unitOfWork.Address
                .GetAll(a => a.ApplicationUserId == user.Id)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.Id)
                .FirstOrDefault();

            var vm = new ProfileVM
            {
                ProfileId = profile.Id,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                FullName = profile.FullName ?? string.Empty,
                IsEmailConfirmed = user.EmailConfirmed,
                Address = savedAddress?.Street ?? profile.Address ?? string.Empty,
                City = savedAddress?.City ?? profile.City ?? string.Empty,
                State = savedAddress?.State ?? profile.State ?? string.Empty,
                PostalCode = savedAddress?.PostalCode ?? profile.PostalCode ?? string.Empty,
                SavedAddresses = _unitOfWork.Address
                    .GetAll(a => a.ApplicationUserId == user.Id)
                    .OrderByDescending(a => a.IsDefault)
                    .ThenByDescending(a => a.Id).ToList()
            };

            return View(vm);
        }

        // ── POST ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileVM model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            // Update phone
            user.PhoneNumber = model.PhoneNumber;
            await _userManager.UpdateAsync(user);

            // ✅ FIX: UnitOfWork se update
            var profile = _unitOfWork.UserProfile.FirstOrDefault(x => x.Id == model.ProfileId)
                       ?? _unitOfWork.UserProfile.FirstOrDefault(x => x.UserId == user.Id);

            if (profile == null)
            {
                profile = new UserProfile { UserId = user.Id };
                _unitOfWork.UserProfile.Add(profile);
            }

            profile.FullName = model.FullName;
            profile.Address = model.Address;
            profile.City = model.City;
            profile.State = model.State;
            profile.PostalCode = model.PostalCode;
            _unitOfWork.Save();

            // Sync Address table
            if (!string.IsNullOrWhiteSpace(model.Address) && !string.IsNullOrWhiteSpace(model.City))
            {
                var existingAddress = _unitOfWork.Address
                    .GetAll(a => a.ApplicationUserId == user.Id)
                    .OrderByDescending(a => a.IsDefault).ThenByDescending(a => a.Id)
                    .FirstOrDefault();

                if (existingAddress != null)
                {
                    existingAddress.Name = model.FullName;
                    existingAddress.PhoneNumber = model.PhoneNumber;
                    existingAddress.Street = model.Address;
                    existingAddress.City = model.City;
                    existingAddress.State = model.State;
                    existingAddress.PostalCode = model.PostalCode;
                    existingAddress.IsDefault = true;
                }
                else
                {
                    _unitOfWork.Address.Add(new Address
                    {
                        ApplicationUserId = user.Id,
                        Name = model.FullName,
                        PhoneNumber = model.PhoneNumber,
                        Street = model.Address,
                        City = model.City,
                        State = model.State,
                        PostalCode = model.PostalCode,
                        IsDefault = true
                    });
                }
                _unitOfWork.Save();
            }

            TempData["ToastMessage"] = "Profile updated successfully ✓";
            return RedirectToAction(nameof(Index));
        }

        // ── Send Verification Email ──
        [HttpPost]
        public async Task<IActionResult> SendVerificationEmail()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Index");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebUtility.UrlEncode(token);
            var callbackUrl = Url.Action(
                "ConfirmEmail", "Profile",
                new { userId = user.Id, token = encodedToken },
                Request.Scheme);

            _ = _emailSender.SendEmailAsync(
                user.Email!,
                "Verify your email – TrendClothing",
                $"Click here to verify your email:<br/><a href='{callbackUrl}'>Verify Email</a>");

            TempData["ToastMessage"] = "Verification email sent 📧";
            return RedirectToAction("Index");
        }

        // ── Confirm Email ──
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
                return RedirectToAction("Index", "Home");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var decodedToken = WebUtility.UrlDecode(token);
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            TempData["ToastMessage"] = result.Succeeded
                ? "Email verified successfully ✅"
                : "Invalid or expired verification link ❌";

            return RedirectToAction("Index");
        }
    }
}