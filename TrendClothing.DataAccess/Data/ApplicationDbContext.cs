using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TrendClothing.Models;
using TrendClothing.Models;

namespace TrendClothing.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }
        public DbSet<Brand> brands { get; set; }
        public DbSet<Size> Sizes { get; set; }
        public DbSet<Color> colors { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<OrderHeader> OrderHeaders { get; set; }
        public DbSet<OrderDetails> OrderDetails { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }



        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);

        //    modelBuilder.Entity<Product>()
        //        .HasOne(p => p.Category)
        //        .WithMany()
        //        .HasForeignKey(p => p.CategoryId)
        //        .OnDelete(DeleteBehavior.Restrict);

        //    modelBuilder.Entity<Product>()
        //        .HasOne(p => p.ProductType)
        //        .WithMany()
        //        .HasForeignKey(p => p.ProductTypeId)
        //        .OnDelete(DeleteBehavior.Restrict);

        //    modelBuilder.Entity<Product>()
        //        .HasOne(p => p.Brand)
        //        .WithMany()
        //        .HasForeignKey(p => p.BrandId)
        //        .OnDelete(DeleteBehavior.Restrict);
        //}
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<IdentityUser>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(450);
            });

            builder.Entity<IdentityRole>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(450);
            });
        }

    }
}


    
