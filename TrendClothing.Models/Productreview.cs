// ✅ NEW FEATURE: Product Reviews
// Step 1: Add this model
// Step 2: Add to ApplicationDbContext: public DbSet<ProductReview> ProductReviews { get; set; }
// Step 3: Add IRepository<ProductReview> to UnitOfWork
// Step 4: Migration: add-migration AddProductReviews → update-database

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendClothing.Models
{
    public class ProductReview
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        [ValidateNever]
        public Product Product { get; set; }

        [Required]
        public string ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; }

        // Only users who actually bought it can review (check via OrderDetails)
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? ReviewText { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Soft-moderation: admin can hide spam reviews
        public bool IsVisible { get; set; } = true;
    }
}