#nullable disable
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Encodings.Web;
using TrendClothing.DataAccess.Repository;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;
using TrendClothing.Utility;
using ApplicationUser = TrendClothing.Models.ApplicationUser;

namespace TrendClothing.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitofWork _unitOfWork;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager,
            IUnitofWork unitOfWork)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        [TempData]
        public string ToastMessage { get; set; }

        [TempData]
        public string ToastColor { get; set; }

        public class InputModel
        {
            [Required, EmailAddress]
            public string Email { get; set; }

            [Required]
            [Phone]
            [Display(Name = "Phone Number")]
            public string PhoneNumber { get; set; }

            [Required]
            [Display(Name = "Country Code")]
            public string CountryCode { get; set; }

            [Required, DataType(DataType.Password)]
            public string Password { get; set; }

            [DataType(DataType.Password), Compare("Password")]
            public string ConfirmPassword { get; set; }

            [Required]
            public string Name { get; set; }

            public string Address { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string PostalCode { get; set; }

            [NotMapped]
            public string Role { get; set; }

            public IEnumerable<SelectListItem> RoleList { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            Input = new InputModel()
            {
                RoleList = _roleManager.Roles
                    .Where(r => r.Name != SD.Role_Idividual && r.Name != SD.Role_Admin)
                    .Select(r => new SelectListItem
                    {
                        Text = r.Name,
                        Value = r.Name
                    })
            };
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                if (!Input.PhoneNumber.All(char.IsDigit))
                {
                    ModelState.AddModelError(
                        "Input.PhoneNumber",
                        "Only digits allowed in phone number"
                    );
                    return Page();
                }

                var user = new ApplicationUser
                {
                    Name = Input.Name,
                    UserName = Input.Email,
                    PhoneNumber = Input.CountryCode + Input.PhoneNumber,
                    Address = Input.Address,
                    City = Input.City,
                    State = Input.State,
                    PostalCode = Input.PostalCode
                };

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user, Input.Password);
                await _userManager.AddToRoleAsync(user, SD.Role_Idividual);

                if (result.Succeeded)
                {
                    // Roles ensure karo
                    string[] roles = { SD.Role_Admin, SD.Role_Employee, SD.Role_Idividual };

                    var profile = new UserProfile
                    {
                        UserId = user.Id,
                        FullName = Input.Name,
                        Address = Input.Address,
                        City = Input.City,
                        State = Input.State,
                        PostalCode = Input.PostalCode
                    };
                    _unitOfWork.UserProfile.Add(profile);
                    _unitOfWork.Save();

                    foreach (var role in roles)
                    {
                        if (!await _roleManager.RoleExistsAsync(role))
                            await _roleManager.CreateAsync(new IdentityRole(role));
                    }

                    int totalUsers = _userManager.Users.Count();

                    if (totalUsers == 1)
                    {
                        await _userManager.AddToRoleAsync(user, SD.Role_Admin);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(Input.Role))
                            await _userManager.AddToRoleAsync(user, Input.Role);
                        else
                            await _userManager.AddToRoleAsync(user, SD.Role_Idividual);
                    }

                    // ✅ Welcome email
                    try
                    {
                        // ✅ Fire-and-forget
                        _ = _emailSender.SendEmailAsync(
                            Input.Email,
                            "Welcome to Trend Clothing 🎉",
                            $"Hi {Input.Name},<br/><br/>Your account has been created successfully.<br/>Happy Shopping! 🛍️"
                        );
                    }
                    catch { }

                    // ✅ Verification email — background mein bhejo
                    try
                    {
                        var userId = await _userManager.GetUserIdAsync(user);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                            protocol: Request.Scheme
                        );

                        await _emailSender.SendEmailAsync(
                            Input.Email,
                            "Verify your email – TrendClothing",
                            $"Hi {Input.Name},<br/><br/>Please verify your email by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.<br/><br/>You need to verify your email before placing an order."
                        );
                    }
                    catch { }

                    // ✅ Seedha login — verification email background mein chala gaya
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    ToastMessage = "Registration successful 🎉 Please verify your email to place orders.";
                    ToastColor = "#198754";
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    ToastMessage = "Registration failed ❌ Please try again";
                    ToastColor = "red";
                }
            }

            return Page();
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
                throw new NotSupportedException("Email not supported.");

            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}