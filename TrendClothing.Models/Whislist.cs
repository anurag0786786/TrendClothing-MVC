// ✅ NEW FEATURE: Wishlist Model
// Migration: add-migration AddWishlist

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendClothing.Models
{
    public class Wishlist
    {
        public int Id { get; set; }

        public string ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        [ValidateNever]
        public Product Product { get; set; }

        public DateTime AddedOn { get; set; } = DateTime.UtcNow;
    }
}