using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Stripe;
using TrendClothing.Data;
using TrendClothing.DataAccess.Repository;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Utility;

var builder = WebApplication.CreateBuilder(args);

// ── DB ──────────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ── IDENTITY ─────────────────────────────────────────────────────────────────
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        // ✅ Slightly stronger password policy
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ── MVC + RAZOR ──────────────────────────────────────────────────────────────
var mvcBuilder = builder.Services.AddControllersWithViews();

// ✅ Sirf Development mein RazorRuntimeCompilation — views change karne pe restart nahi chahiye
// Production mein automatically off rahega — speed pe koi asar nahi
if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}

builder.Services.AddRazorPages();

// ── CUSTOM SERVICES ───────────────────────────────────────────────────────────
builder.Services.AddScoped<IUnitofWork, UnitOfWork>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<EmailTemplateRenderer>();
builder.Services.AddScoped<CloudinaryService>();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<TwilioSettings>(builder.Configuration.GetSection("TwilioSettings"));
builder.Services.AddScoped<ISmsSender, SmsSender>();

// ── SESSION ───────────────────────────────────────────────────────────────────
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ── COOKIE CONFIG ─────────────────────────────────────────────────────────────
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.LogoutPath = "/Identity/Account/Logout";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

// ── STRIPE ────────────────────────────────────────────────────────────────────
StripeConfiguration.ApiKey = builder.Configuration["StripeSettings:SecretKey"];

// ── GOOGLE / FACEBOOK AUTH ───────────────────────────────────────────────────
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    })
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["Authentication:Facebook:AppId"]!;
        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"]!;
    });

// ── EMAIL (Resend SMTP) ───────────────────────────────────────────────────────
// API Key appsettings mein Authentication:ResendApiKey se aata hai
// EmailSender SmtpClient use karta hai smtp.resend.com se

// ── DATA PROTECTION ───────────────────────────────────────────────────────────
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/tmp/dataprotection-keys"));

// ✅ NEW: RESPONSE CACHING (for future use with [ResponseCache])
builder.Services.AddResponseCaching();

var app = builder.Build();

// ── PIPELINE ─────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ✅ FIX: Reverse proxy (Render/Railway/Nginx) ke liye — ReturnUrl aur login redirect live pe kaam kare
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

// ✅ Static files with caching headers
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=86400");
    }
});

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseResponseCaching();

// ✅ FIX: Area route PEHLE register karo — warna Wishlist/Review controllers match nahi hote
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();