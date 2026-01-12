using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrendClothing.Data;
using TrendClothing.Models;
using TrendClothing.Models.ViewModels;
using TrendClothing.Utility;

namespace TrendClothing.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UserController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ================= USER LIST =================
        public IActionResult Index()
        {
            return View();
        }

        // ================= CREATE USER (GET) =================
        public IActionResult Create()
        {
            ViewBag.RoleList = new List<string>
            {
                SD.Role_Employee,
                SD.Role_Admin
            };
            return View();
        }

        // ================= CREATE USER (POST) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserVM vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RoleList = new List<string>
                {
                    SD.Role_Employee,
                    SD.Role_Admin
                };
                return View(vm);
            }

            var user = new ApplicationUser
            {
                Name = vm.Name,
                Email = vm.Email,
                UserName = vm.Email
            };

            var result = await _userManager.CreateAsync(user, vm.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, vm.Role);
                TempData["success"] = "User created successfully";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            ViewBag.RoleList = new List<string>
            {
                SD.Role_Employee,
                SD.Role_Admin
            };

            return View(vm);
        }

        // ================= API : GET ALL =================
        #region APIs
        [HttpGet]
        public IActionResult GetAll()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var users = _context.ApplicationUsers.ToList();
            var userRoles = _context.UserRoles.ToList();
            var roles = _context.Roles.ToList();

            var data = users.Select(u =>
            {
                var roleId = userRoles.FirstOrDefault(x => x.UserId == u.Id)?.RoleId;
                var roleName = roles.FirstOrDefault(x => x.Id == roleId)?.Name;

                return new
                {
                    id = u.Id,
                    name = u.Name,
                    email = u.Email,
                    roles = roleName,
                    lockoutEnd = u.LockoutEnd
                };
            })
            // ✅ ONLY EMPLOYEE
            // ❌ ADMIN SELF HIDDEN
            .Where(x =>
                x.roles == SD.Role_Employee &&
                x.id != currentUserId
            )
            .ToList();

            return Json(new { data });
        }


        // ================= LOCK / UNLOCK =================
        [HttpPost]
        public IActionResult LockUnlock([FromBody] string id)
        {
            var user = _context.ApplicationUsers.FirstOrDefault(u => u.Id == id);
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            if (user.Id == currentUserId)
            {
                return Json(new { success = false, message = "You cannot lock yourself" });
            }

            if (user.LockoutEnd != null && user.LockoutEnd > DateTime.Now)
                user.LockoutEnd = DateTime.Now;
            else
                user.LockoutEnd = DateTime.Now.AddYears(100);

            _context.SaveChanges();

            return Json(new
            {
                success = true,
                message = user.LockoutEnd > DateTime.Now
                    ? "User locked successfully"
                    : "User unlocked successfully"
            });
        }
        #endregion
    }
}
