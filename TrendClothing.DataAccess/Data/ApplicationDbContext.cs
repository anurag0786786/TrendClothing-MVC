using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TrendClothing.Models;

namespace TrendClothing.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ─── Catalog ──────────────────────────────────────────────────────────
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Size> Sizes { get; set; }
        public DbSet<Color> Colors { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }

        // ─── Users ───────────────────────────────────────────────────────────
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Address> Addresses { get; set; }

        // ─── Commerce ────────────────────────────────────────────────────────
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<OrderHeader> OrderHeaders { get; set; }
        public DbSet<OrderDetails> OrderDetails { get; set; }

        // ─── Site Content ─────────────────────────────────────────────────────
        public DbSet<HeroImage> HeroImages { get; set; }
        public DbSet<SiteImage> SiteImages { get; set; }

        // ✅ NEW: Wishlist
        public DbSet<Wishlist> Wishlists { get; set; }

        // ✅ NEW: Product Reviews
        public DbSet<ProductReview> ProductReviews { get; set; }

        // ✅ FIX: Removed old "brands" and "colors" (lowercase) DbSet names
        // (were inconsistently named — now all PascalCase)

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Identity max length constraints
            builder.Entity<IdentityUser>(e => e.Property(x => x.Id).HasMaxLength(450));
            builder.Entity<IdentityRole>(e => e.Property(x => x.Id).HasMaxLength(450));

            // ✅ Wishlist: one user can wishlist each product only once
            builder.Entity<Wishlist>()
                .HasIndex(w => new { w.ApplicationUserId, w.ProductId })
                .IsUnique();

            // ✅ ProductReview: one review per user per product
            builder.Entity<ProductReview>()
                .HasIndex(r => new { r.ApplicationUserId, r.ProductId })
                .IsUnique();

            // ✅ FIX: Cascade delete restrictions to prevent accidental data loss
            builder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Product>()
                .HasOne(p => p.Brand)
                .WithMany()
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProductVariant>()
                .HasOne(v => v.Product)
                .WithMany()
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // variants delete when product is deleted
        }
    }
}